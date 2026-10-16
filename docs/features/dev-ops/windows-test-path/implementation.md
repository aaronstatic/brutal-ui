# Windows Test Path - Implementation Plan

**Epic:** dev-ops (feature 3 of 3)
**Status:** Ready for Review
**Started:** 2026-10-16
**Target Completion:** TBD
**Last Updated:** 2026-10-16 14:20

---

## Quality Bar

**Target quality:** Production-grade developer tooling. The author runs `scripts\test-windows.ps1` on the Windows machine after each pull in each later epic. The later `github-ci` epic reuses it. No agent can run the scripts on Windows, so each defect costs a round trip through the author. A false PASS, a hang, or a script that does not start costs the most.

- **One exit code tells the truth:** The script exits 0 only when the build, the tests, and the smoke run all pass. Any failure gives a non-zero exit code. The exit code of the failed step passes through. A GPU stall ends in a time limit, not a hang.
- **Quiet on success, specific on failure:** Success prints one line. A failure prints the failed step, its exit code, the last 30 lines of its log, and the path of the full log. A failed check prints the problem and the exact fix.
- **No hang, no orphan:** A time-out or `Ctrl+C` stops the full process tree of the step (`cmd.exe`, `dotnet`, `HelloPanel`). No `HelloPanel` process stays alive.
- **A plain prompt:** The scripts run in Windows PowerShell 5.1 (part of Windows) and in PowerShell 7. They need the .NET 10 SDK and nothing else. No Visual Studio, no module, no install.
- **The shared contract, exactly:** The scripts use the exact commands of the shared contract of `dev-ops/solution-skeleton`. The settings, the output shape, and the log folder mirror `scripts/test-linux.sh`.
- **No change to the state of the session or the repository:** After a run, the PowerShell session has the same current folder and the same environment variables. A run writes only under the git-ignored `artifacts/` folder.

---

## Executive Summary

This feature gives the author one short procedure for a Windows test run after a git pull. `scripts\test-windows.ps1` builds the solution, runs the unit tests, checks for the Vulkan loader, and runs the `HelloPanel` sample for 120 frames in a visible window. It mirrors the output contract, the settings, and the log folder pattern of `scripts/test-linux.sh`. `scripts\build.ps1` runs the build only. The runbook `docs/testing-windows.md` holds the procedure (pull `main`, run the test script, look at the sample, report the result) and a checklist for the manual look. A `.gitattributes` file keeps the shell scripts at LF on both operating systems. No agent can run anything on Windows. The agent checks the scripts statically on Linux. The author runs each Windows check by hand.

---

## User Personas Alignment

No `docs/user-personas.md` file exists. The personas below come from the epic docs.

**Target Users:**
- [x] **The author on Windows** (pulls `main`, runs one script, looks at one window, and reports one result in under five minutes)
- [x] **The agent on Linux** (writes the scripts, checks them statically, and reads the report of the author)
- [ ] **The agent on Windows** (not served here. No agent runs on Windows. The `github-ci` epic adds a Windows runner later)

**User Subsets Served:**
- [x] **Simple** - The author follows one short runbook. Each failure message names its fix.
- [x] **Hands-off** - One script call with no argument and no input, and one exit code.

**UX Approach:**
- One script call with no argument. Two environment variables change the frame count and the smoke time limit. They have the same names as on Linux.
- The output on success is one line. The output on failure is one header line, at most 30 lines of the failed step, and the log path.
- The runbook ends with a fixed report block. The author copies it to the agent.

---

## Goals

### Primary Goals
1. Give the author one PowerShell script that proves the build, the unit tests, and the Vulkan path on the Windows machine, in a visible window.
2. Give the author one runbook that takes under five minutes from the pull to the report.
3. Close the open Windows items of `dev-ops/solution-skeleton`: the first Windows run of `HelloPanel`, and the CR-1 minimize path.
4. Keep the shell scripts at LF on both operating systems with `.gitattributes`, and change no tracked file.

### Success Criteria
- [ ] `git check-attr eol -- scripts/test-linux.sh scripts/check-gpu.sh` prints `lf` for both files.
- [ ] A renormalize with the new `.gitattributes` changes no tracked file.
- [ ] (MANUAL) `.\scripts\test-windows.ps1` exits 0 on the Windows machine and prints exactly one line.
- [ ] (MANUAL) `.\scripts\test-windows.ps1` exits non-zero on a build failure, a smoke failure, a smoke time-out, and a failed check.
- [ ] (MANUAL) `.\scripts\build.ps1` exits 0, and exits non-zero on a build failure.
- [ ] (MANUAL) The author completes the procedure of `docs/testing-windows.md` in under five minutes.

---

## Architecture Overview

Two PowerShell scripts, one runbook, and one `.gitattributes` file. `test-windows.ps1` runs a fixed list of steps. Each step runs through `cmd.exe`, which sends the output of the step to one log file. The first failed step stops the run. `build.ps1` is a separate script. It shows the `dotnet build` output on the console and exits with its code.

### File Tree (end state)

```
brutal-ui/
├── .gitattributes                   # phase 1
├── scripts/
│   ├── test-linux.sh                # existing, no change
│   ├── check-gpu.sh                 # existing, no change
│   ├── test-windows.ps1             # phase 1, phase 2 adds preflight, loader, and interrupt cleanup
│   └── build.ps1                    # phase 2
├── docs/
│   ├── testing-windows.md           # phase 3
│   └── features/dev-ops/windows-test-path/context.md   # each phase records results
└── artifacts/test-windows/          # written by each run on Windows, ignored by git (artifacts/)
    └── build.log, test.log, smoke.log
```

No other file changes. `.gitignore` already holds `artifacts/`.

### `.gitattributes` (phase 1)

```
# Git stores each text file with LF line endings. A checkout on Windows can
# convert the text files to CRLF. The shell scripts stay at LF on each
# operating system, because bash fails on a CR character.
* text=auto
*.sh text eol=lf
```

- Every file that Git tracks today holds LF only (a search for a CR character in the `.sln`, `.csproj`, `.props`, `.cs`, `.md`, `.json`, `.config`, and `.sh` files found none). So the rules change no file in the index. Task 1.2 proves it.
- `* text=auto` lets Git find binary files by itself. A checkout on Windows gives CRLF to the text files. Windows PowerShell, `dotnet`, and the classic `.sln` format accept CRLF and LF.

### The Steps of `scripts\test-windows.ps1`

| # | Step | Command or check | Failure exit code | Phase |
|---|------|------------------|-------------------|-------|
| 1 | `preflight` | `dotnet` exists, `dotnet --version` starts with `10.`, `BRUTAL_UI_SMOKE_TIMEOUT` is valid, `BRUTAL_UI_FRAMES` holds no special character, the OS is Windows, the log folder resets | 1 | 2 |
| 2 | `build` | `dotnet build brutal-ui.sln` | the exit code of `dotnet` | 1 |
| 3 | `test` | `dotnet test --solution brutal-ui.sln` | the exit code of `dotnet` | 1 |
| 4 | `loader` | `vulkan-1.dll` exists in `[Environment]::SystemDirectory` | 1 | 2 |
| 5 | `smoke` | `dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build -- --frames "<FRAMES>"`, with a time limit | the exit code of the sample, or 124 on a time-out | 1 |

No `gpu` step exists on Windows. `check-gpu.sh` reads Linux driver files only. The `loader` step comes after the tests, so a driver problem does not hide the unit test result. No `-c` option appears. The build, the test, and the smoke run all use the Debug configuration, so `--no-build` finds the build output.

### The Step Runner

Windows PowerShell 5.1 has two traps for a native command. First, a redirect of the standard error of a native command (`2>&1`, `2>`, `*>`) turns each line into an error record. With `$ErrorActionPreference = 'Stop'` the first line stops the script. Second, the `>` operator writes UTF-16 files. So the script never redirects the output of a native command in PowerShell. `cmd.exe` does the redirect:

```
Start-Process -FilePath $env:ComSpec `
    -ArgumentList "/d /s /c `"<command> > artifacts\test-windows\<step>.log 2>&1`"" `
    -WorkingDirectory <repository root> -NoNewWindow -PassThru
```

- `/d` skips the AutoRun commands of `cmd.exe`. `/s` removes only the outer quotes, so the command stays as written. `cmd.exe /c` exits with the exit code of the command.
- The runner reads `$p.Handle` right after the start. Without it, `Start-Process -PassThru` can return an empty `ExitCode`.
- The runner waits in a loop of `$p.WaitForExit(200)`. The loop lets PowerShell react to `Ctrl+C`, and it measures the time limit of the smoke step with a `Stopwatch`.
- On a time-out, the runner stops the full tree with `taskkill.exe /PID <id> /T /F`, run through the same `cmd.exe` pattern with `>nul 2>&1`. .NET Framework (Windows PowerShell 5.1) has no `Process.Kill(true)`.
- A `finally` block stops the tree when the step still runs (a `Ctrl+C` or another stop), and writes the interrupted line.
- The log paths are relative to the repository root and hold no space. So the command needs no inner quotes.

### Output Contract

| Case | Standard output | Standard error | Exit code |
|------|-----------------|----------------|-----------|
| Success | One line: `test-windows: PASS build, test, smoke (<FRAMES> frames) in <S> s` | nothing | 0 |
| A step fails | nothing | `test-windows: FAIL at <step> (exit <rc>)`, then the last 30 lines of `artifacts\test-windows\<step>.log`, then `test-windows: full log: artifacts\test-windows\<step>.log` | `<rc>` of the step |
| A smoke time-out | nothing | the header adds `, timed out after <SMOKE_TIMEOUT> s` | 124 |
| A check fails (`preflight`, `loader`) | nothing | `test-windows: FAIL at <step>: <problem>`, then `test-windows: fix: <command>` | 1 |
| `Ctrl+C` in a step | nothing | `test-windows: FAIL at <step>: interrupted` | non-zero (PowerShell sets the code. Task 2.7 records the real value) |

`<S>` is the whole number of seconds of the run, rounded down. The script writes to standard error with `[Console]::Error.WriteLine`, not with `Write-Error`, so no error record text appears.

### Output Contract of `scripts\build.ps1`

| Case | Console | Exit code |
|------|---------|-----------|
| Success | The `dotnet build` output only | 0 |
| The build fails | The `dotnet build` output, then `build: FAIL at build (exit <rc>)` on standard error | `<rc>` of `dotnet` |
| A check fails | `build: FAIL at preflight: <problem>`, then `build: fix: <command>` on standard error | 1 |

`build.ps1` checks only `dotnet` and its version. It runs on any operating system with PowerShell, so an agent with `pwsh` can run it on Linux.

### Settings

| Variable | Meaning | Default | Check |
|----------|---------|---------|-------|
| `BRUTAL_UI_FRAMES` | The number of frames of the smoke run | 120 | Only letters, digits, `+`, and `-`. The sample checks the number, so `0` or `abc` gives exit 2. |
| `BRUTAL_UI_SMOKE_TIMEOUT` | The time limit of the smoke step in seconds | 60 | A whole number above 0 that fits in an `[int]` |

An empty variable counts as unset, as `${VAR:-default}` does in `bash`.

---

## Implementation Phases

Each phase is a slice: one narrow path that works end to end, with a demo line that a person can check. Section 11 of `.beast-mode/phase-handoff.md` holds the rules. The requirements leave the demo of phase 1 open. Phase 1 uses the recommended line of open scope question 1 in Notes.

No agent can run the scripts on Windows. Each phase ends with static checks on Linux. Each Windows check goes into the section "Needs Human Verification" of `context.md` as a MANUAL item.

### Phase 1: The Windows Test Script Runs Build, Test, and the Smoke Run
**Goal:** Write `.gitattributes`, so the pull on Windows gives working files. Write `scripts\test-windows.ps1` with the step runner, the build, test, and smoke steps, the time limit, the failure output, and the one-line summary.
**Demo:** the author runs `.\scripts\test-windows.ps1` in a PowerShell window at the repository root on Windows, sees the `HelloPanel` window open and close, then sees one line that starts with `test-windows: PASS`, and `$LASTEXITCODE` is 0.
**Needs:** none
**Agent tier:** ADVANCED (opus)

**Tier note:** This phase is load-bearing. The step runner is the pattern that phase 2, `build.ps1`, and the `github-ci` epic build on. No agent can run it. Its correctness depends on the Windows PowerShell 5.1 rules of the section "The Step Runner": no PowerShell redirect of a native command, the handle read, the wait loop, and the tree kill. Follow that section exactly. Do not try other designs. When a rule of that section looks wrong, stop and report.

**Tasks:**
- [ ] 1.1 Write `.gitattributes` at the repository root with the exact text of the Architecture Overview: three comment lines, `* text=auto`, and `*.sh text eol=lf`. Use LF line endings and ASCII text only.
- [ ] 1.2 Check `.gitattributes` in one shell command, and record each result in `context.md`. (a) `git ls-files --eol | grep -E '^i/(crlf|mixed)'`: expect no output. (b) Clone the repository into the session scratchpad (`git clone -q --no-local /home/aaronstatic/dev/brutal-ui <scratch>/eol-check`), copy `.gitattributes` into the clone, run `git -C <scratch>/eol-check add --renormalize .`, then `git -C <scratch>/eol-check diff --cached --name-only`: expect no output. Do not run `git add --renormalize` in the real repository. It also stages the unrelated changes of the working tree. (c) `git check-attr text eol -- scripts/test-linux.sh scripts/check-gpu.sh brutal-ui.sln`: expect `eol: lf` for both `.sh` files, and `text: auto` with `eol: unspecified` for the `.sln`. (d) `grep -c $'\r' scripts/*.sh`: expect 0 for each file. (e) `git status --porcelain`: expect no modified tracked file outside the feature folder and the new files.
- [ ] 1.3 Write the frame of `scripts\test-windows.ps1`. ASCII text only (Windows PowerShell 5.1 reads a file with no BOM as ANSI). Start with a comment-based help block in ASD-STE100 prose: the purpose, the steps, the output, the exit codes, and the two environment variables (mirror the header of `scripts/test-linux.sh`). Then `#Requires -Version 5.1`, `Set-StrictMode -Version Latest`, and `$ErrorActionPreference = 'Stop'`. Do not set `$PSNativeCommandUseErrorActionPreference`. Read `$root = Split-Path -Parent $PSScriptRoot`. Put the full body in `Push-Location -LiteralPath $root` and `try { ... } finally { Pop-Location }`, because `Set-Location` in a script changes the folder of the caller session. Read `$frames` from `$env:BRUTAL_UI_FRAMES` (default `120`) and `$smokeTimeout` from `$env:BRUTAL_UI_SMOKE_TIMEOUT` (default `60`, converted to `[int]`). Set `$logDir = 'artifacts\test-windows'`. Save the old values of `DOTNET_NOLOGO` and `MSBUILDDISABLENODEREUSE`, set both to `1`, and restore both in the `finally` block (Decision 6). Delete and create `$logDir` at the start of each run. Start a `[System.Diagnostics.Stopwatch]`.
- [ ] 1.4 Write the helper `Write-CheckFail <step> <problem> <fix>`. It writes the two check lines of the output contract with `[Console]::Error.WriteLine` and calls `exit 1`. (`exit` in a function ends the script. The `finally` block still runs.)
- [ ] 1.5 Write the helper `Invoke-Step <Name> <Command> [<TimeoutSeconds>]` per the section "The Step Runner". On a non-zero exit code, write the failure block of the output contract to standard error: the header, the lines of `Get-Content -LiteralPath <log> -Tail 30`, and the log path. For a time-out on the `smoke` step, set the exit code to 124 and add `, timed out after <N> s` to the header. Then `exit` with the code. Run `taskkill.exe` only through `cmd.exe` with `>nul 2>&1`, so its output never reaches the console.
- [ ] 1.6 Add the build and test steps: `Invoke-Step build 'dotnet build brutal-ui.sln'` and `Invoke-Step test 'dotnet test --solution brutal-ui.sln'`. Use these exact forms. Do not add `-c`. Do not use the positional form `dotnet test brutal-ui.sln`. It fails in Microsoft.Testing.Platform mode.
- [ ] 1.7 Add the smoke step: `Invoke-Step smoke "dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build -- --frames `"$frames`"" $smokeTimeout`. Keep the forward slashes of the contract path. `dotnet` accepts them on Windows. The quotes around the value keep a `cmd.exe` special character literal until phase 2 adds the check. Then write exactly one line to standard output with `[Console]::Out.WriteLine`: `test-windows: PASS build, test, smoke (<FRAMES> frames) in <S> s`, with `<S>` from `[math]::Floor($sw.Elapsed.TotalSeconds)`. Write nothing else. The script reads the exit code of the sample, not its output.
- [ ] 1.8 Find the name of the Vulkan loader that `Brutal.Vulkan` loads on Windows. Search `samples/HelloPanel/bin/Debug/net10.0/Brutal.Vulkan.dll` with `strings -a` and `strings -el` (when `strings` exists) for `vulkan`. Use `ilspycmd` only when it already exists. Install nothing. Record the result in `context.md`, section "Native Dependencies". When the package loads `vulkan` with no Windows mapping to `vulkan-1`, .NET probes `vulkan.dll`, and the Windows run fails with `DllNotFoundException`. Then write the finding to `context.md`, section "Open Questions", and to the inbox, and continue. The script does not change. The Windows run is the final check.
- [ ] 1.9 Check statically on Linux, in one shell command, and record each result in `context.md`. (a) `grep -nP '[^\x00-\x7F]' scripts/test-windows.ps1 .gitattributes`: expect no output. (b) `command -v pwsh`. When `pwsh` exists, parse the script: `pwsh -NoProfile -Command '$e = $null; [void][System.Management.Automation.Language.Parser]::ParseFile("scripts/test-windows.ps1", [ref]$null, [ref]$e); $e.Count'`: expect `0`. When `Invoke-ScriptAnalyzer` exists, run it and fix each warning and error. Install nothing. When `pwsh` does not exist, record "not run" and add the parse check to the MANUAL list. (c) Search the script for the three exact contract strings of Definition of Done I1, for ` -c `, and for `dotnet test brutal-ui.sln`. (d) Read the script once for each native command, and confirm that no PowerShell redirect (`>`, `2>`, `*>`, `|`) follows it. (e) Add the MANUAL checks of this phase to the section "Needs Human Verification" of `context.md`: the demo, Definition of Done F4 to F9, and Q7.

**Estimated Steps:** about 54 (9 tasks, about six steps for one task)

**Files (3):** `.gitattributes`, `scripts/test-windows.ps1`, `docs/features/dev-ops/windows-test-path/context.md`

**Acceptance Criteria:**
- [ ] `git check-attr eol -- scripts/test-linux.sh scripts/check-gpu.sh` prints `lf` for both files, and the renormalize in the scratch clone changes no tracked file.
- [ ] `scripts/test-windows.ps1` holds only ASCII characters and the exact build, test, and smoke commands of the shared contract.
- [ ] No native command in the script has a PowerShell redirect. `cmd.exe` writes each log.
- [ ] The parse check gives 0 errors, or the check is on the MANUAL list when `pwsh` does not exist.
- [ ] `context.md` records the name of the Vulkan loader that `Brutal.Vulkan` loads on Windows, or records that the agent could not read it.
- [ ] (MANUAL) The demo passes on the Windows machine.

---

### Phase 2: Preflight, Loader Check, Interrupt Cleanup, and the Build Script
**Goal:** Add the preflight checks and the `loader` step to `test-windows.ps1`, so a missing tool or a missing driver gives a fix in place of a late error. Stop the process tree on `Ctrl+C`. Write `scripts\build.ps1`.
**Demo:** the author runs `.\scripts\build.ps1` in a PowerShell window at the repository root on Windows, sees the `dotnet build` output end with `Build succeeded`, and `$LASTEXITCODE` is 0.
**Needs:** Phase 1
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 2.1 Add the `preflight` step to `test-windows.ps1`, before the log folder reset. Run the checks in this order, so an agent with `pwsh` on Linux can test the first four. (1) `Get-Command dotnet -CommandType Application -ErrorAction SilentlyContinue`: on no result, `Write-CheckFail preflight 'dotnet does not exist' 'winget install Microsoft.DotNet.SDK.10'`. (2) `dotnet --version`: set `$ErrorActionPreference = 'Continue'` for this one call only, run `$v = & dotnet --version 2>$null`, and set it back to `'Stop'`. When `$v` does not start with `10.`, fail with `dotnet version <v or unknown> is not version 10` and the same fix. (3) `BRUTAL_UI_SMOKE_TIMEOUT`: it must match `^[1-9][0-9]*$` and pass `[int]::TryParse`. Otherwise fail with `BRUTAL_UI_SMOKE_TIMEOUT <value> is not a whole number above 0` and the fix `Set BRUTAL_UI_SMOKE_TIMEOUT to a number of seconds, for example 60.`. (4) `BRUTAL_UI_FRAMES`: it must match `^[0-9A-Za-z+-]+$`. Otherwise fail with `BRUTAL_UI_FRAMES <value> holds a character that is not a letter, a digit, a plus sign, or a minus sign` and the fix `Set BRUTAL_UI_FRAMES to a whole number above 0, for example 120.`. (5) `[Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT`: fail with `this script runs on Windows only` and the fix `Run scripts/test-linux.sh on Linux.`. Move the `[int]` conversion of task 1.3 after check 3.
- [ ] 2.2 Make the log folder reset a check. Run the delete with `-ErrorAction Stop` in `try` and `catch`. On a failure, call `Write-CheckFail preflight 'artifacts\test-windows is in use' 'Close each HelloPanel window. Run dotnet build-server shutdown. Run the script again.'`.
- [ ] 2.3 Add the `loader` step after `test` and before `smoke`. Test `Join-Path ([Environment]::SystemDirectory) 'vulkan-1.dll'` with `Test-Path -LiteralPath`. On a failure, call `Write-CheckFail loader 'the Vulkan loader vulkan-1.dll does not exist in <folder>' 'Install or update the GPU driver. The driver installs vulkan-1.dll.'`. Add a comment that names `docs/features/dev-ops/solution-skeleton/context.md`, section "Native Dependencies", and the result of task 1.8.
- [ ] 2.4 Add the interrupt cleanup to `Invoke-Step`. Put the wait loop in `try` and `finally`. When the `finally` block runs and the step process has not exited, stop the tree with `taskkill.exe` (task 1.5 pattern), and write `test-windows: FAIL at <step>: interrupted` to standard error. Do not print the PASS line after an interrupt. Do not catch the stop. PowerShell ends the script after the `finally` block. Update the help block: the preflight checks, the `loader` step, the interrupt line, and the exit codes.
- [ ] 2.5 Write `scripts\build.ps1`. ASCII text only. A comment-based help block in ASD-STE100 prose, `#Requires -Version 5.1`, `Set-StrictMode -Version Latest`, and `$ErrorActionPreference = 'Stop'`. Use the same `Push-Location` frame and the same save and restore of `DOTNET_NOLOGO` as task 1.3. Do not set `MSBUILDDISABLENODEREUSE` (the build writes to the console, not to a file). Copy checks 1 and 2 of task 2.1, with the prefix `build:`. Then run `& dotnet build brutal-ui.sln` with no redirect, so its output goes to the console. Read `$LASTEXITCODE`. On a non-zero code, write `build: FAIL at build (exit <rc>)` to standard error. Exit with the code. The script holds no Windows-only call.
- [ ] 2.6 Check statically on Linux, in one shell command, and record each result in `context.md`. (a) The ASCII check of task 1.9 for both scripts. (b) When `pwsh` exists: the parse check for both scripts. Then `pwsh -NoProfile -File scripts/build.ps1; echo "exit=$?"`: expect the build output and `exit=0`. Then `BRUTAL_UI_SMOKE_TIMEOUT=abc pwsh -NoProfile -File scripts/test-windows.ps1; echo "exit=$?"`: expect the timeout check lines and `exit=1`. Then `BRUTAL_UI_FRAMES='1 2' pwsh -NoProfile -File scripts/test-windows.ps1; echo "exit=$?"`: expect the frames check lines and `exit=1`. Then `pwsh -NoProfile -File scripts/test-windows.ps1 >"<scratch>/out" 2>"<scratch>/err"; echo "exit=$?"`: expect `test-windows: FAIL at preflight: this script runs on Windows only` in `err`, an empty `out`, `exit=1`, and no `artifacts/test-windows` folder. Then a temporary `src/BrutalUi.Core/Broken.cs` with `class Broken {`, and `pwsh -NoProfile -File scripts/build.ps1; echo "exit=$?"`: expect `build: FAIL at build (exit 1)` and `exit=1`. Delete the file, and confirm with `git status --porcelain src/`. (c) When `pwsh` does not exist, record "not run", and add each check of (b) to the MANUAL list in PowerShell form. (d) Search both scripts for `Set-ExecutionPolicy`, `devenv`, `msbuild.exe`, and `vswhere`: expect no command. `winget` appears only inside fix text.
- [ ] 2.7 Add the MANUAL checks of this phase to "Needs Human Verification" in `context.md`: the demo, Definition of Done F10 to F12, and the exit code that PowerShell gives after `Ctrl+C`. Ask the author to record that code in the report block.

**Estimated Steps:** about 42 (7 tasks, about six steps for one task)

**Files (3):** `scripts/test-windows.ps1`, `scripts/build.ps1`, `docs/features/dev-ops/windows-test-path/context.md`

**Acceptance Criteria:**
- [ ] `test-windows.ps1` runs the preflight checks in the order of task 2.1, and each check prints its exact problem and fix (read check, and the `pwsh` run when `pwsh` exists).
- [ ] `test-windows.ps1` checks for `vulkan-1.dll` after the tests and before the smoke run.
- [ ] A stop during a step leaves no step process alive and prints the interrupted line (read check, MANUAL run).
- [ ] `build.ps1` runs `dotnet build brutal-ui.sln`, passes its exit code through, and prints the failure line on a failure.
- [ ] (MANUAL) The demo passes on the Windows machine.

---

### Phase 3: The Windows Testing Runbook
**Goal:** Write `docs/testing-windows.md` with the one-time setup, the five-minute procedure, the manual look checklist, the report block, and the failure table. Record the Windows system dependencies in `context.md`.
**Demo:** the author opens `docs/testing-windows.md` on the Windows machine, follows its procedure from the pull to the report block, and finishes in under five minutes with the `test-windows: PASS` line and each item of the look checklist checked.
**Needs:** Phase 2
**Agent tier:** STANDARD (sonnet)

**Tasks:**
- [ ] 3.1 Write `docs/testing-windows.md` in ASD-STE100 prose, with the section order of `docs/testing-linux.md` where it fits. Sections: (1) Purpose, two sentences. (2) One-time setup: a table with the columns Item, Check, and Fix. Rows: Git (`git --version`, `winget install Git.Git`), a clone of the repository (one `git clone` line, with the URL of the author as a placeholder), the .NET 10 SDK (`dotnet --version` prints `10.`, `winget install Microsoft.DotNet.SDK.10`), the script policy (per open scope question 2: `Get-ExecutionPolicy -Scope CurrentUser`, `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned`), and the Vulkan loader (`Test-Path "$env:SystemRoot\System32\vulkan-1.dll"` prints `True`, install or update the GPU driver). (3) The procedure, numbered, each step with its command: pull (`git switch main`, then `git pull --ff-only`), run the test script (`.\scripts\test-windows.ps1`, the window opens for about two seconds, the PASS line), look at the sample (`dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build`, then the checklist of section 4), and report (section 5). (4) The look checklist: the window title is `HelloPanel`. The window shows the text `Hello, brutal-ui` on a dark background. Minimize the window, restore it, and see the window draw again. Minimize it again, and close it from the taskbar (right-click the taskbar button, then Close window). `$LASTEXITCODE` is 0. `Get-Process HelloPanel -ErrorAction SilentlyContinue` prints nothing. (5) The report block: a fenced text block with the date, the output of `git rev-parse --short HEAD`, the output of `dotnet --version`, the PASS or FAIL lines, and one line for each checklist item. Say where the author sends it (open scope question 4). (6) Exit codes: the table of the output contract. (7) Settings: the two variables with their defaults, the PowerShell form (`$env:BRUTAL_UI_FRAMES = '300'`), and the removal (`Remove-Item Env:BRUTAL_UI_FRAMES`). Say that a variable stays set in the session until its removal. (8) Failures and fixes: one row for each `FAIL at <step>` header, with the first thing to read and the fix. Include the `DllNotFoundException` for `vulkan` and a time-out. (9) Build only: `.\scripts\build.ps1` and its output contract. (10) Limits: no CI, no Visual Studio, no installer, no screenshot, the Debug configuration only, one run at a time.
- [ ] 3.2 Update `context.md`. Section "System Dependencies": a table of each Windows item that the scripts use: Windows PowerShell 5.1 or PowerShell 7, `cmd.exe`, `taskkill.exe`, the .NET 10 SDK, Git for Windows, `vulkan-1.dll` (GPU driver). Section "Native Dependencies": the feature adds no native file to the repository. `glfw3.dll` and `imgui.dll` come from the packages, and `vulkan-1.dll` comes from the driver (link to the table of `docs/features/dev-ops/solution-skeleton/context.md`). Add the result of task 1.8. Section "Related": the look checklist covers the open items "Phase 2 Windows" and "CR-1 Windows" of `docs/features/dev-ops/solution-skeleton/context.md`.
- [ ] 3.3 Verify. Run each non-MANUAL step of the Verification Method of the Definition of Done. Compare each command and each output string in `docs/testing-windows.md` with the two scripts, character for character. Run `python3 /home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py --fail-over 2.5` on `docs/testing-windows.md` when the file exists, and fix each finding over the limit. Run `scripts/test-linux.sh` one time to prove no regression on Linux. Record the results in `context.md`. Add the MANUAL steps and the phase demo to "Needs Human Verification".

**Estimated Steps:** about 18 (3 tasks, about six steps for one task)

**Files (2):** `docs/testing-windows.md`, `docs/features/dev-ops/windows-test-path/context.md`

**Acceptance Criteria:**
- [ ] `docs/testing-windows.md` holds the ten sections of task 3.1.
- [ ] Each command and output string in the runbook matches the scripts exactly.
- [ ] The look checklist holds the minimize, restore, minimize, and close-from-taskbar steps of CR-1, and the exit code check.
- [ ] `context.md` lists the Windows system dependencies and states that no native file enters the repository.
- [ ] `scripts/test-linux.sh` still prints its PASS line.
- [ ] (MANUAL) The demo passes in under five minutes.

---

## Key Technical Decisions

### Confirmed Assumptions

- `.gitattributes`: `dev-ops/windows-test-path` owns `.gitattributes` and adds the `*.sh text eol=lf` rule. (Scope answer 3 of `dev-ops/linux-dev-environment`, answered by the author on 2026-10-16.)

### Decision 1: Mirror the Linux script pattern in PowerShell
**Context:** The chosen approach names a mirror of `scripts/test-linux.sh`. The `github-ci` epic later reads both script sets.
**Decision:** The same step names where a step exists on both systems, the same settings with the same defaults, the same output contract shape with the prefix `test-windows:`, the same exit code pass-through, and the same log folder pattern (`artifacts\test-windows\<step>.log`). The Linux-only steps `gpu` and the `libvulkan.so` check do not carry over. A `vulkan-1.dll` check takes the place of the second.
**Rationale:** One reader understands both scripts. The agent reads the report of the author with the same rules as a Linux run.
**Alternatives Considered:** One cross-platform PowerShell script for both systems. Rejected: the Linux script is finished and verified, and `pwsh` is not a dependency on Linux.

### Decision 2: Windows PowerShell 5.1 as the floor
**Context:** The requirements name "a plain PowerShell prompt". Windows ships Windows PowerShell 5.1. PowerShell 7 needs an install.
**Decision:** `#Requires -Version 5.1`. The scripts use no PowerShell 7 feature (`$IsWindows`, `Process.Kill($true)`, `Start-Process -Environment`, the `&&` operator, ternary operators). The OS check reads `[Environment]::OSVersion.Platform`. The files hold ASCII only.
**Rationale:** No install on the Windows machine. The scripts also run in PowerShell 7. Open scope question 3 asks the author.
**Alternatives Considered:** PowerShell 7 only. It gives simpler code, but it adds an install step.

### Decision 3: `cmd.exe` writes the logs
**Context:** In Windows PowerShell 5.1, a redirect of the standard error of a native command turns each line into an error record, and with `$ErrorActionPreference = 'Stop'` it stops the script. The `>` operator writes UTF-16 files.
**Decision:** Each step runs as `cmd.exe /d /s /c "<command> > <log> 2>&1"` through `Start-Process -NoNewWindow -PassThru`. PowerShell never redirects a native command, except the one `2>$null` of the version check under `Continue` (task 2.1).
**Rationale:** The log keeps the order of the lines and the bytes of `dotnet`. The exit code passes through `cmd.exe`. The process object gives the time limit and the tree kill.
**Alternatives Considered:** `Start-Process -RedirectStandardOutput` and `-RedirectStandardError`. Rejected: two files lose the line order, and the tail of 30 lines needs a merge. `System.Diagnostics.Process` with event handlers. Rejected: more code, and PowerShell event handlers run on other threads.

### Decision 4: A time limit and a tree kill for the smoke step
**Context:** The `HelloPanel` frame waits have no time limit (solution-skeleton CR-3 declined). `dotnet run` starts `HelloPanel` as a child process.
**Decision:** A wait loop of 200 ms with a `Stopwatch`. On a time-out, `taskkill.exe /PID <id> /T /F` stops `cmd.exe`, `dotnet`, and `HelloPanel`. The step exits 124. No second code exists (Linux 137), because `taskkill /F` stops at once.
**Rationale:** No hang and no orphan window. The exit code 124 matches Linux.

### Decision 5: The OS check comes last in the preflight
**Context:** No agent can run the script on Windows. An agent with `pwsh` on Linux can run the preflight.
**Decision:** The order is `dotnet`, the version, the time limit, the frames, then the OS. The log folder reset comes after the preflight.
**Rationale:** On Linux, the first four checks run before the OS check stops the run. The agent then tests the check messages and the exit code 1 with no Windows machine. The script writes nothing on Linux, because the reset comes after the OS check.

### Decision 6: `MSBUILDDISABLENODEREUSE=1` for the run of `test-windows.ps1`
**Context:** `dotnet build` leaves MSBuild node processes alive (the Linux run showed seven). On Windows, a process that holds a log file open blocks the delete of `artifacts\test-windows` at the next run.
**Decision:** `test-windows.ps1` sets `MSBUILDDISABLENODEREUSE=1` and `DOTNET_NOLOGO=1` for the run, and restores the old values after the run. The command strings of the contract do not change. A failed folder delete prints a fix with `dotnet build-server shutdown` (task 2.2).
**Rationale:** The nodes end with the build, so the next run can delete the folder. The cost is a slower second build, a few seconds.
**Alternatives Considered:** The option `-nodeReuse:false` on the command line. Rejected: it changes the exact contract string.

### Decision 7: A check of the `BRUTAL_UI_FRAMES` characters
**Context:** Linux passes `BRUTAL_UI_FRAMES` to the sample unchecked, so a bad value gives exit 2. On Windows, the value goes into a `cmd.exe` command line, where `&`, `|`, `%`, and `"` have a meaning.
**Decision:** The preflight accepts letters, digits, `+`, and `-` only. The sample still checks the number, so `0`, `-5`, and `abc` still give exit 2.
**Rationale:** The check keeps the exit 2 path of Linux and keeps the command line safe.

### Decision 8: `.gitattributes` with two rules
**Context:** The Confirmed Assumptions block gives `*.sh text eol=lf`. The requirements ask that the scripts and the solution work on both systems.
**Decision:** `* text=auto` and `*.sh text eol=lf`. No rule for `.ps1`, `.sln`, or `.cs`.
**Rationale:** Each tracked file holds LF today, so the rules change no file in the index. Windows PowerShell, `dotnet`, and the classic `.sln` accept both line endings. An `eol=crlf` rule for `.sln` or `.ps1` makes the Linux working files differ from a fresh checkout (YAGNI). Open scope question 5 asks the author.

### Decision 9: Each script stands alone
**Context:** `test-windows.ps1` and `build.ps1` share two preflight checks.
**Decision:** Each script holds its own copy of the checks. No shared `.ps1` file and no module.
**Rationale:** The chosen approach rejects a module. The copy is about ten lines. A later reader and the `github-ci` epic read one file for one script. `test-windows.ps1` holds the exact build string of the contract, so it does not call `build.ps1`.

### Decision 10: The runbook runs the sample directly for the manual look
**Context:** The contract gives a no-argument mode of `HelloPanel` for the manual look on Windows.
**Decision:** The runbook calls `dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build` after the test script. The test script has no look option.
**Rationale:** The test script already built the Debug output. One more command is shorter than a new script option. Open scope question 6 asks the author.

---

## Risks & Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| `Brutal.Vulkan` loads `vulkan` on Windows, so .NET probes `vulkan.dll`, which does not exist. The smoke run exits 1 with `DllNotFoundException`. | Medium | High | Task 1.8 reads the name from the package. The runbook failure table has the row. The finding goes to BRUTAL, as the Linux `libvulkan.so` finding did. |
| A Windows PowerShell 5.1 trap (a native redirect, an empty `ExitCode`, a blocked `Ctrl+C`) breaks the runner. No agent can see it. | Medium | High | Decision 3 and the section "The Step Runner" fix each rule. Phase 1 runs on the advanced tier. The MANUAL list covers each path. |
| The execution policy of the machine blocks `.\scripts\test-windows.ps1`. | High | Medium | The one-time setup row of the runbook (open scope question 2). The runbook also names the `-ExecutionPolicy Bypass -File` form. |
| A time-out leaves a `HelloPanel` window alive. | Low | Medium | `taskkill /T /F` on the tree. F7 checks `Get-Process HelloPanel` after the run. |
| A process holds a log file open, and the next run cannot delete the log folder. | Medium | Medium | Decision 6. Task 2.2 prints the fix. F9 runs the script two times in a row. |
| `.gitattributes` changes a tracked file at the next commit. | Low | Medium | Task 1.2 proves no change with `git ls-files --eol` and a renormalize in a scratch clone. |
| An existing Windows clone keeps old line endings after the pull. | Low | Low | Git converts a file when the pull changes it. No Windows tool reads the `.sh` files. Notes item 6. |
| The author closes the window during the smoke run. The sample exits 0, and the PASS line reports the requested frame count. | Low | Low | The runbook says: do not touch the window during the test script. `smoke.log` holds the real count. |
| `pwsh` does not exist on this Linux machine, so no agent parses the scripts. | Medium | Medium | Tasks 1.9 and 2.6 move each check to the MANUAL list. The author runs the parse check on Windows. |
| The first restore on Windows needs the network and takes longer than five minutes. | Medium | Low | The runbook says: the first run downloads the packages. The five-minute target counts from the second run. |
| A variable stays set in the PowerShell session after a test. | Medium | Low | The runbook shows `Remove-Item Env:<name>` after each change. |
| Two runs at the same time share `artifacts\test-windows`. | Low | Low | The runbook says: one run at a time. |

---

## Dependencies

### Internal Dependencies
- `dev-ops/solution-skeleton` (complete): `brutal-ui.sln`, the test projects, the `HelloPanel` sample, its command line contract, and the shared contract commands.
- `dev-ops/linux-dev-environment` (finished, parallel, no blocking dependency): the pattern to mirror. Its Integration Criterion I2 checks the `*.sh` rule of this feature. This feature changes none of its files.

### External Dependencies
- The .NET 10 SDK on the Windows machine. `global.json` pins `10.0.100` with `rollForward: latestFeature`, so any 10.0 SDK works. The author installs it one time.
- Windows PowerShell 5.1 (part of Windows), `cmd.exe`, and `taskkill.exe` (part of Windows).
- Git for Windows.
- A GPU driver that installs `vulkan-1.dll` in `System32`.
- The NuGet package cache, or network access to nuget.org for the first restore.
- On Linux, for the static checks only: `git`, `grep`, and `strings` (binutils). `pwsh` when it exists. The plan installs nothing.

---

## Definition of Done

All criteria below must pass. An independent evaluator runs the non-MANUAL steps from the repository root `/home/aaronstatic/dev/brutal-ui` on Linux. "MANUAL" marks a criterion that needs the author on the Windows machine. A MANUAL criterion passes when the author reports the result.

### Functional Criteria
- [ ] F1. `.gitattributes` exists at the repository root. Its non-comment lines are exactly `* text=auto` and `*.sh text eol=lf`.
- [ ] F2. `git check-attr eol -- scripts/test-linux.sh scripts/check-gpu.sh` prints `scripts/test-linux.sh: eol: lf` and `scripts/check-gpu.sh: eol: lf`.
- [ ] F3. `git ls-files --eol` shows no `i/crlf` and no `i/mixed` entry. A renormalize in a scratch clone with the new `.gitattributes` stages no file.
- [ ] F4. (MANUAL) At the repository root on Windows, `.\scripts\test-windows.ps1` opens the `HelloPanel` window, then exits with `$LASTEXITCODE` 0. Standard output holds exactly one line that matches `test-windows: PASS build, test, smoke (120 frames) in <N> s`. Standard error is empty.
- [ ] F5. (MANUAL) The same call by the full script path from another folder gives the same result. After the call, `Get-Location` prints the folder of the call.
- [ ] F6. (MANUAL) With `$env:BRUTAL_UI_FRAMES = '0'`, the script exits 2. Standard error starts with `test-windows: FAIL at smoke (exit 2)`, holds a `Usage:` line, and ends with `test-windows: full log: artifacts\test-windows\smoke.log`.
- [ ] F7. (MANUAL) With `$env:BRUTAL_UI_FRAMES = '100000000'` and `$env:BRUTAL_UI_SMOKE_TIMEOUT = '5'`, the script exits 124, and the header holds `timed out after 5 s`. After the run, `Get-Process HelloPanel -ErrorAction SilentlyContinue` prints nothing.
- [ ] F8. (MANUAL) A temporary `src\BrutalUi.Core\Broken.cs` with `class Broken {` makes the script exit non-zero with `FAIL at build`. The tail shows a `CS` error. The author deletes the file after the check.
- [ ] F9. (MANUAL) Two runs in a row both print the PASS line. The second run deletes and writes `artifacts\test-windows` with no error.
- [ ] F10. (MANUAL) `.\scripts\build.ps1` shows the `dotnet build` output, ends with `Build succeeded`, and exits 0. With the file of F8, it exits non-zero, and its last line is `build: FAIL at build (exit 1)`.
- [ ] F11. With `BRUTAL_UI_SMOKE_TIMEOUT` set to `abc`, the script prints `test-windows: FAIL at preflight: BRUTAL_UI_SMOKE_TIMEOUT abc is not a whole number above 0` and the fix line, and exits 1. With `BRUTAL_UI_FRAMES` set to `1 2`, it prints the frames check lines and exits 1. On Linux with `pwsh`, the plain call prints `test-windows: FAIL at preflight: this script runs on Windows only` and exits 1. (The agent runs these with `pwsh` when it exists. Otherwise the checks are MANUAL on Windows, with no OS line.)
- [ ] F12. (MANUAL) `Ctrl+C` during the smoke step prints `test-windows: FAIL at smoke: interrupted`, gives a non-zero `$LASTEXITCODE`, and leaves no `HelloPanel` process. The author records the exit code.
- [ ] F13. `docs/testing-windows.md` exists and holds the ten sections of task 3.1.
- [ ] F14. (MANUAL) The author completes the procedure of `docs/testing-windows.md`, from the pull to the report block, in under five minutes on the second run. Each item of the look checklist passes, and the sample exits 0 after the close from the taskbar.

### Quality Criteria
- [ ] Q1. `grep -nP '[^\x00-\x7F]' scripts/test-windows.ps1 scripts/build.ps1 .gitattributes` prints nothing.
- [ ] Q2. Both scripts hold a comment-based help block, `#Requires -Version 5.1`, `Set-StrictMode -Version Latest`, and `$ErrorActionPreference = 'Stop'`.
- [ ] Q3. The parse check of task 1.9 gives 0 errors for both scripts (with `pwsh` on Linux, or MANUAL on Windows with the same `Parser]::ParseFile` call).
- [ ] Q4. No native command in `test-windows.ps1` has a PowerShell redirect, except `& dotnet --version 2>$null` under `Continue`. Each step command and each `taskkill.exe` call runs inside a `cmd.exe /d /s /c` string or a `cmd.exe /d /c` string.
- [ ] Q5. A search of both scripts finds no `Set-ExecutionPolicy`, `devenv`, `msbuild.exe`, or `vswhere`. `winget` appears only inside fix text.
- [ ] Q6. On Linux, `git status --porcelain` shows no path under `artifacts/`, `bin/`, or `obj/`. (MANUAL) The same on Windows after a run.
- [ ] Q7. (MANUAL) After a run, `$env:DOTNET_NOLOGO` and `$env:MSBUILDDISABLENODEREUSE` hold their old values in the session.
- [ ] Q8. The help blocks of both scripts and `docs/testing-windows.md` use ASD-STE100 prose. `ste-lint.py --fail-over 2.5` on the runbook exits 0.
- [ ] Q9. `scripts/test-linux.sh` on Linux still prints `test-linux: PASS build, test, gpu, smoke (120 frames) in <N> s` and exits 0.

### Integration Criteria
- [ ] I1. `scripts/test-windows.ps1` holds these exact strings: `dotnet build brutal-ui.sln`, `dotnet test --solution brutal-ui.sln`, and `dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build -- --frames`. `scripts/build.ps1` holds `dotnet build brutal-ui.sln`. Neither script holds a ` -c ` option or `dotnet test brutal-ui.sln`.
- [ ] I2. `scripts/test-windows.ps1` reads `BRUTAL_UI_FRAMES` (default 120) and `BRUTAL_UI_SMOKE_TIMEOUT` (default 60), uses the prefix `test-windows:`, and writes to `artifacts\test-windows`.
- [ ] I3. Each command and output string in `docs/testing-windows.md` matches the scripts exactly.
- [ ] I4. The look checklist of the runbook covers the open items "Phase 2 Windows" and "CR-1 Windows" of `docs/features/dev-ops/solution-skeleton/context.md`.
- [ ] I5. `context.md` of this feature lists the Windows system dependencies, the name of the Vulkan loader of task 1.8, and states that no native file enters the repository.
- [ ] I6. `git diff --name-only` and `git status --porcelain` show no change to `scripts/test-linux.sh`, `scripts/check-gpu.sh`, `docs/testing-linux.md`, or any file under `src/`, `tests/`, or `samples/`.

### Verification Method

Run steps 1 to 9 from `/home/aaronstatic/dev/brutal-ui` on Linux. Use the session scratchpad folder for temporary files. Steps 10 to 19 are MANUAL on the Windows machine, in a PowerShell window at the root of the clone, after `git switch main` and `git pull --ff-only`.

1. `grep -v '^#' .gitattributes; git check-attr eol -- scripts/test-linux.sh scripts/check-gpu.sh`. Expect F1 and F2.
2. `git ls-files --eol | grep -E '^i/(crlf|mixed)'`. Expect no output. Then clone into the scratchpad, copy `.gitattributes`, run `git add --renormalize .` in the clone, and `git diff --cached --name-only`. Expect no output (F3).
3. `grep -nP '[^\x00-\x7F]' scripts/test-windows.ps1 scripts/build.ps1 .gitattributes`. Expect no output (Q1). Read the first lines of both scripts for Q2.
4. `command -v pwsh`. When it exists, run the parse check of task 1.9 on both scripts. Expect `0` for each (Q3).
5. When `pwsh` exists, run the four `pwsh` calls of task 2.6 (b). Expect F11 and the `build.ps1` results. Delete `Broken.cs`, and confirm with `git status --porcelain src/`.
6. Search `scripts/test-windows.ps1` for the strings of I1 and I2. Search both scripts for ` -c `, `dotnet test brutal-ui.sln`, `Set-ExecutionPolicy`, `devenv`, `msbuild.exe`, `vswhere`, and `winget` (Q5). Read each native call for Q4.
7. `env -u DISPLAY -u WAYLAND_DISPLAY scripts/test-linux.sh; echo "exit=$?"`. Expect the PASS line and `exit=0` (Q9).
8. `git status --porcelain; git diff --name-only`. Expect Q6 and I6.
9. Read `docs/testing-windows.md`. Confirm F13, I3, and I4. Run `ste-lint.py --fail-over 2.5` on it (Q8). Read `context.md` for I5.
10. (MANUAL) When `pwsh` did not exist on Linux, run the parse check in PowerShell: `$e = $null; [void][System.Management.Automation.Language.Parser]::ParseFile("$PWD\scripts\test-windows.ps1", [ref]$null, [ref]$e); $e.Count`, and the same for `build.ps1`. Expect `0` (Q3).
11. (MANUAL) `cmd /c "powershell -NoProfile -ExecutionPolicy Bypass -File scripts\test-windows.ps1 >%TEMP%\out.txt 2>%TEMP%\err.txt"; $LASTEXITCODE; Get-Content $env:TEMP\out.txt; (Get-Item $env:TEMP\err.txt).Length`. Expect `0`, one PASS line, and `0` (F4).
12. (MANUAL) `$env:DOTNET_NOLOGO; $env:MSBUILDDISABLENODEREUSE`, then `Set-Location $env:TEMP; & "<clone>\scripts\test-windows.ps1"; $LASTEXITCODE; Get-Location`, then the two variables again. Expect the PASS line, `0`, the `TEMP` folder, and the same variable values as before (F5, Q7). Return to the clone.
13. (MANUAL) `$env:BRUTAL_UI_FRAMES = '0'; .\scripts\test-windows.ps1; $LASTEXITCODE; Remove-Item Env:BRUTAL_UI_FRAMES`. Expect F6.
14. (MANUAL) `$env:BRUTAL_UI_FRAMES = '100000000'; $env:BRUTAL_UI_SMOKE_TIMEOUT = '5'; .\scripts\test-windows.ps1; $LASTEXITCODE; Get-Process HelloPanel -ErrorAction SilentlyContinue; Remove-Item Env:BRUTAL_UI_FRAMES, Env:BRUTAL_UI_SMOKE_TIMEOUT`. Expect F7.
15. (MANUAL) Write `src\BrutalUi.Core\Broken.cs` with `class Broken {`. Run `.\scripts\test-windows.ps1; $LASTEXITCODE`, then `.\scripts\build.ps1; $LASTEXITCODE`. Expect F8 and the failure part of F10. Delete the file. `git status --porcelain` shows no change.
16. (MANUAL) `.\scripts\build.ps1; $LASTEXITCODE`. Expect the success part of F10. Then `.\scripts\test-windows.ps1` two times in a row. Expect F9. Then `git status --porcelain`. Expect no `artifacts` path (Q6).
17. (MANUAL) When `pwsh` did not exist on Linux, set `$env:BRUTAL_UI_SMOKE_TIMEOUT = 'abc'` and run the script, then `$env:BRUTAL_UI_FRAMES = '1 2'` and run the script. Remove both variables. Expect F11 (no OS line on Windows).
18. (MANUAL) `$env:BRUTAL_UI_FRAMES = '100000000'; .\scripts\test-windows.ps1`. Press `Ctrl+C` while the window shows. Run `$LASTEXITCODE; Get-Process HelloPanel -ErrorAction SilentlyContinue; Remove-Item Env:BRUTAL_UI_FRAMES`. Expect F12. Record the exit code.
19. (MANUAL) Follow `docs/testing-windows.md` from the pull to the report block with a timer. Expect F14. Send the report block to the agent.

---

## Testing Strategy

### Unit Tests
- None. The feature holds no C# code. The scripts have no unit test framework (YAGNI).

### Static Tests (agent, on Linux)
- The ASCII check, the PowerShell parse check (when `pwsh` exists), and the search for the contract strings.
- The `.gitattributes` checks: `git check-attr`, `git ls-files --eol`, and a renormalize in a scratch clone.
- With `pwsh`: `build.ps1` runs end to end on Linux, and the preflight of `test-windows.ps1` runs up to the OS check.
- The Linux regression run of `scripts/test-linux.sh`.

### Integration Tests (MANUAL, on Windows)
- The script run itself is the integration test: build, unit tests, loader check, and the Vulkan smoke run in a visible window.
- Failure injection with no edit of the repository: `BRUTAL_UI_FRAMES=0` (exit code pass-through), `BRUTAL_UI_SMOKE_TIMEOUT=5` (time limit and tree kill), and the two preflight values.
- Failure injection with a temporary edit: one broken source file for the build step. The author deletes it after the check.

### Manual Testing
- The look checklist of the runbook, with the CR-1 minimize path of `dev-ops/solution-skeleton`.
- `Ctrl+C` during the smoke step.
- The five-minute procedure with a timer.

### Not Tested Here
- A test failure in `dotnet test`. The build failure proves the same runner path.
- A missing `vulkan-1.dll`. The author does not remove a driver file. A read check covers the `loader` step.
- A hosted Windows runner. The `github-ci` epic owns it.

---

## Related Documentation

- **Requirements:** `docs/features/dev-ops/windows-test-path/requirements.md`
- **Epic:** `docs/features/dev-ops/epic-overview.md`, `docs/features/dev-ops/epic-requirements.md`
- **Siblings:** `docs/features/dev-ops/solution-skeleton/implementation.md` (section "The Shared Contract with the Sibling Features"), `docs/features/dev-ops/solution-skeleton/context.md` (sections "Native Dependencies", "HelloPanel Command Line Contract", and "Needs Human Verification"), `docs/features/dev-ops/linux-dev-environment/implementation.md`, `docs/features/dev-ops/linux-dev-environment/context.md`, `scripts/test-linux.sh`, `docs/testing-linux.md`
- **Rules:** `AGENTS.md`, `.beast-mode/phase-handoff.md` sections 1 and 11, `.beast-mode/tool-rules.md`
- **Prose lint:** `/home/aaronstatic/.claude/skills/asd-ste100/scripts/ste-lint.py`
- **External:** https://dotnet.microsoft.com/download/dotnet/10.0 (the .NET 10 SDK), https://www.wingetly.io/apps/microsoft/dot-net-sdk-10 (the winget id `Microsoft.DotNet.SDK.10`, not checked against the Microsoft winget docs)

---

## Notes

### Open Scope Questions

The author answered all six questions on 2026-10-16 and chose the recommended option each time. The plan uses those options.

1. **The demo of phase 1** (rule 12 of the slice rules).
   - (Recommended) `the author runs .\scripts\test-windows.ps1 in a PowerShell window at the repository root on Windows, sees the HelloPanel window open and close, then sees one line that starts with test-windows: PASS, and $LASTEXITCODE is 0.`
   - `the author runs powershell -NoProfile -ExecutionPolicy Bypass -File scripts\test-windows.ps1 on Windows, sees one line that starts with test-windows: PASS, and $LASTEXITCODE is 0.` (works with any execution policy, but is longer)
   - `the agent runs git check-attr eol -- scripts/test-linux.sh on Linux and sees eol: lf.` (an agent can check it, but it proves no Windows path)
2. **The script call and the execution policy.** The default policy of Windows client blocks a `.ps1` file.
   - (Recommended) A one-time `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned` in the setup table, then the short call `.\scripts\test-windows.ps1`. A file from `git clone` has no download mark, so `RemoteSigned` runs it.
   - No policy change. Each call is `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\test-windows.ps1`.
   - A `.cmd` wrapper for each script that makes the `Bypass` call. Adds two files.
3. **The PowerShell version.**
   - (Recommended) Windows PowerShell 5.1 and PowerShell 7 both. No install.
   - PowerShell 7.4 only, with a one-time `winget install Microsoft.PowerShell`. Simpler code (`Kill($true)`, `$IsWindows`), and the call becomes `pwsh -File ...`.
4. **How the author reports the result.**
   - (Recommended) The author pastes the report block of the runbook into the agent session, or into the `inbox.md` of the active feature.
   - The author checks the boxes in `docs/features/dev-ops/human-testing.md` and pushes a commit.
   - Both.
5. **The scope of `.gitattributes`.**
   - (Recommended) Two rules: `* text=auto` and `*.sh text eol=lf`.
   - Add `*.ps1 text eol=crlf` and `*.sln text eol=crlf` for Visual Studio habits. The Linux working files then differ from a fresh checkout until a new checkout.
   - Only `*.sh text eol=lf`. Each machine keeps its own `core.autocrlf` rule for the other files.
6. **The manual look of the sample.**
   - (Recommended) The runbook calls `dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build` after the test script.
   - A `-Look` switch on `test-windows.ps1` that runs the sample with no argument after the PASS line.

### Other Notes

1. **No shell access in planning.** The planner had no shell, so it did not run `command -v pwsh`. Tasks 1.9 and 2.6 run it and move each check to the MANUAL list when `pwsh` does not exist.
2. **The Vulkan loader name on Windows is not confirmed.** `docs/features/dev-ops/solution-skeleton/context.md` names `vulkan-1.dll`, but nobody ran the sample on Windows, and the Linux finding shows that `Brutal.Vulkan` loads the name `vulkan`. A search of the package DLL with the planner tools found no readable string. Task 1.8 reads it with `strings`. When the name is wrong, the Windows run fails, and the finding goes to BRUTAL.
3. **The `Ctrl+C` exit code.** PowerShell sets the exit code after a stop. Linux gives 130. Windows can give another non-zero code. Task 2.7 and F12 record the real value. The runbook then names it.
4. **The PASS line reports the requested frame count.** When the author closes the window early, the sample exits 0 with fewer frames. The Linux script has the same rule. `smoke.log` holds the real count.
5. **The open Windows items of `dev-ops/solution-skeleton`.** The look checklist covers "Phase 2 Windows" and "CR-1 Windows". After the report, the author checks those items in `docs/features/dev-ops/human-testing.md`. This feature does not edit the files of the sibling.
6. **An old Windows clone.** A clone from before `.gitattributes` keeps its working files until a pull changes them. No Windows tool reads the `.sh` files, so no action is necessary. To refresh all files, run `git rm --cached -r -q .` and then `git reset --hard` on a clean clone.
7. **The `github-ci` epic.** A hosted Windows runner has no GPU and no visible desktop. That epic must skip the `loader` and `smoke` steps or add a software driver. This plan does not prepare it (YAGNI).
8. **Agent discovery.** `AGENTS.md` names no script. A later change can add one line for both runbooks. This feature does not edit `AGENTS.md`.
9. **The epic overview.** After this feature lands, `/update-epic dev-ops` records the `.gitattributes` rule and the Vulkan loader name under Tech Debt / Findings.
10. **No user personas file.** `docs/user-personas.md` does not exist. The personas of this plan come from the epic docs.

---

*This plan will be updated as implementation progresses. See context.md and tasks.md for current status.*
