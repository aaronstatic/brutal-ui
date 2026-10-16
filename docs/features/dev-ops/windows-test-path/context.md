# dev-ops/windows-test-path - Context & Decisions

**Last Updated:** 2026-10-16 14:20
**Current Phase:** Complete (all three phases and the cross-phase review done)
**Status:** 🟢 Ready for Review

---

## Quick Status

**What's Done:**
- ✅ Implementation plan (`implementation.md`), with the scope answers of the author (the recommended option for each of the six questions)
- ✅ Dev docs scaffolded (`context.md`, `tasks.md`, `inbox.md`)
- ✅ Phase 1, tasks 1.1 to 1.9: `.gitattributes`, `scripts/test-windows.ps1` (build, test, smoke), the static checks, and the loader name `vulkan-1`
- ✅ Phase 2, tasks 2.1 to 2.7: the preflight checks, the `loader` step, the interrupt cleanup, and `scripts/build.ps1` (verifier PASS after fix 1)
- ✅ Phase 3, tasks 3.1 to 3.3: the runbook `docs/testing-windows.md`, the Windows dependencies, and the Linux verification (verifier PASS)

- ✅ Cross-phase review: 6 findings (2 MEDIUM, 4 LOW), all fixed. Verifier `review-verify.md` PASS.

**What's Next:**
- 📋 The author runs the items of `## Needs Human Verification` on the Windows machine (the Phase 3 demo first: it covers F4 and the look checklist)
- 📋 The author sends the report block, with the `Ctrl+C` exit code (F12)
- 📋 The caller commits the work

**Blockers:**
- none. No agent can run the scripts on Windows. Each Windows check goes into `## Needs Human Verification`.

---

## Key Files

### Core Implementation
- `.gitattributes` - Line ending rules (phase 1)
- `scripts/test-windows.ps1` - The full Windows test run: preflight, build, test, loader, smoke (phases 1 and 2)
- `scripts/build.ps1` - The build only (phase 2)
- `docs/testing-windows.md` - The Windows runbook (phase 3)

### Related Files
- `docs/features/dev-ops/windows-test-path/implementation.md` - Implementation plan
- `docs/features/dev-ops/solution-skeleton/context.md` - The shared contract, the `HelloPanel` command line contract, and the Native Dependencies section
- `scripts/test-linux.sh` and `docs/testing-linux.md` - The Linux pattern that this feature mirrors
- The look checklist of `docs/testing-windows.md` covers the open items "Phase 2 Windows" and "CR-1 Windows" of `docs/features/dev-ops/solution-skeleton/context.md`. After the report, the author checks them in `docs/features/dev-ops/human-testing.md`.

---

## Important Decisions

The plan holds decisions 1 to 10 and the scope answers 1 to 6. This section records the decisions made during the build.

- Scope answers (2026-10-16): the author chose the recommended option for each of the six open scope questions. Phase 1 uses the recommended demo. The runbook uses a one-time `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned`. The scripts support Windows PowerShell 5.1 and PowerShell 7. The author pastes the report block into the agent session or the `inbox.md` of the active feature. `.gitattributes` holds two rules. The runbook runs the sample directly for the manual look.
- Tool state on this Linux machine at the start: `pwsh` does not exist. `strings` exists (`/usr/bin/strings`). `ilspycmd` and `shellcheck` do not exist on `PATH`. So the PowerShell parse check and each `pwsh` run of tasks 1.9 and 2.6 go to the MANUAL list.

### Phase 1 (2026-10-16)

- Task 1.2, the `.gitattributes` checks (one shell command):
  - (a) `git ls-files --eol | grep -E '^i/(crlf|mixed)'` printed nothing (grep exit 1). PASS.
  - (b) The scratch clone of `HEAD` with the new `.gitattributes` and `git add --renormalize .` staged no file. `git diff --cached --name-only` printed nothing. The clone showed only `?? .gitattributes`. PASS. The clone holds the committed files only. The untracked feature docs and the new files are LF and ASCII (checked with `grep -c $'\r'` and the ASCII check of task 1.9).
  - (c) `git check-attr text eol`: `scripts/test-linux.sh` and `scripts/check-gpu.sh` give `text: set` and `eol: lf`. `brutal-ui.sln` gives `text: auto` and `eol: unspecified`. PASS.
  - (d) `grep -c $'\r' scripts/*.sh` gave `0` for `test-linux.sh` and `check-gpu.sh`. PASS.
  - (e) `git status --porcelain` showed only `M` on `implementation.md` of this feature and `??` on `.gitattributes`, `scripts/test-windows.ps1`, and the feature docs. No other tracked file changed. PASS.
- Task 1.8, the Vulkan loader name: see "Native Dependencies". The result is `vulkan-1` on Windows. The script does not change.
- Task 1.9, the static checks (one shell command):
  - (a) `grep -nP '[^\x00-\x7F]' scripts/test-windows.ps1 .gitattributes` printed nothing. Both files hold no CR character. PASS.
  - (b) `command -v pwsh` found nothing. The parse check and `Invoke-ScriptAnalyzer` did not run ("not run"). The parse check is on the MANUAL list. A manual review found balanced braces (17 and 17) and parentheses (44 and 44), and no space after a line continuation backtick.
  - (c) Contract strings: `dotnet build brutal-ui.sln` (2 hits: help and step), `dotnet test --solution brutal-ui.sln` (2 hits), `dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build -- --frames` (1 hit). ` -c ` gave 0 hits. `dotnet test brutal-ui.sln` gave 0 hits. `BRUTAL_UI_FRAMES`, `BRUTAL_UI_SMOKE_TIMEOUT`, `test-windows:`, and `artifacts\test-windows` are present (I2). PASS.
  - (d) Each native command runs through `Start-Process -FilePath $env:ComSpec` with a `/d /s /c "..."` string: the step command (line 93) and `taskkill.exe` (line 69, with `>nul 2>&1`). Each `>` and `2>&1` is inside that string, so `cmd.exe` does the redirect. No PowerShell redirect and no pipe follows a native command. `$null = New-Item` is a cmdlet, not a native command. PASS.
  - The ASD-STE100 lint (`ste-lint.py --fail-over 2.5`) of the help block and the comments gave 0.40 findings for each 100 words. PASS.
- Script design notes (inside the rules of "The Step Runner"):
  - A helper `Stop-StepTree` holds the `taskkill.exe` call, so the time-out path and the `finally` block of task 2.4 share one call. It waits up to 30 s for `taskkill.exe`, then up to 10 s for the step process. A plain `WaitForExit()` can hang when `taskkill.exe` fails.
  - The runner keeps the handle of the step process open. Windows then does not give its process ID to a new process, so `taskkill.exe /PID` cannot hit a wrong process.
  - The wait loop is one `while` block with no other statement in it. Task 2.4 wraps it in `try` and `finally` with no rewrite. Task 2.4 must also set a flag after a normal exit, so the `finally` block does not stop a finished process.
  - The failure block reads the log only when it exists (`Test-Path`), so a missing log does not stop the script with an error record.
  - The restore uses `[Environment]::SetEnvironmentVariable(<name>, <old value>)`. A null old value removes the variable, so an unset variable stays unset (Decision 6).
  - The `Stopwatch` starts first inside `try`, so `<S>` counts the full run, as `$SECONDS` does in `test-linux.sh`.
  - `Write-CheckFail` has no caller in phase 1. The preflight and loader steps of phase 2 call it.
- Blast radius: the phase adds two new files only (`.gitattributes` and `scripts/test-windows.ps1`). No file under `src/`, `tests/`, or `samples/` changes. `.gitattributes` changes no file in the index (task 1.2 (b)). It changes the checkout on Windows only: each text file becomes CRLF, and each `.sh` file stays LF.

### Phase 2 (2026-10-16)

- Task 2.1, the preflight: `test-windows.ps1` runs the six checks after the two environment variables are set, inside the `try` block, so `Write-CheckFail` (exit 1) still restores the variables. The order is `dotnet`, the version, the time limit, the frames, the OS, then the log folder. The `[int]` conversion of the time limit now comes from `[int]::TryParse` after check 3 (`$smokeTimeout`). Check 2 sets `Continue` for the one call and restores `Stop` in a `finally` block. A missing or empty version output reads `unknown`. The comparison uses `StartsWith('10.', Ordinal)`.
- Task 2.2: the log folder delete runs with `-ErrorAction Stop` in `try` and `catch`. The catch calls `Write-CheckFail preflight 'artifacts\test-windows is in use' ...`. The `New-Item` call stays outside the `try` block, as the brief names the delete only.
- Task 2.3: the `loader` step comes after `test` and before `smoke`. It tests `vulkan-1.dll` in `[Environment]::SystemDirectory`. The comment names the `solution-skeleton` section "Native Dependencies" and the result of task 1.8.
- Task 2.4: `Invoke-Step` holds the wait loop in `try` and `finally`. A flag `$waitDone` becomes true after the loop ends (a normal exit or a time-out). The `finally` block acts only when `$waitDone` is false and `$p.HasExited` is false. It calls `Stop-StepTree` and writes `test-windows: FAIL at <step>: interrupted`. The flag stops a false "interrupted" line after a time-out when `taskkill.exe` failed. No `catch`, so PowerShell ends the script after the block. The help block now lists the six preflight checks, the `loader` step, the interrupt line, and the exit codes.
- Task 2.5: `scripts/build.ps1` holds its own copy of checks 1 and 2 (prefix `build:`), the `Push-Location` frame, and the save and restore of `DOTNET_NOLOGO`. It sets no `MSBUILDDISABLENODEREUSE`. It runs `& dotnet build brutal-ui.sln` with no redirect, writes `build: FAIL at build (exit <rc>)` on a non-zero code, and exits with that code. It holds no Windows-only call.
- Fix 1 (2026-10-16), after the phase 2 verifier FAIL of gate 4. Three changes in `scripts/test-windows.ps1`:
  - Time limit check (line 220): `-cnotmatch '^[1-9][0-9]*\z'`, with `[int]::TryParse` and the texts unchanged. A trailing line feed now fails.
  - Frames check (line 227): `-cnotmatch '^[0-9A-Za-z+-]+\z'`, with the texts of task 2.1 unchanged. The case-sensitive match blocks the Kelvin sign U+212A. The `\z` anchor blocks a trailing line feed, which would end the `cmd.exe` command line early.
  - `finally` block of `Invoke-Step` (line 142 on): when `$waitDone` is false, the block always writes `test-windows: FAIL at <step>: interrupted`. It calls `Stop-StepTree` only when `$p.HasExited` is false. This replaces the rule of task 2.4 (the line only for a live process), because Ctrl+C reaches `HelloPanel`, `dotnet run`, and `cmd.exe` on the console, and they can exit before the block runs. A normal exit and a time-out still write no interrupted line. No `catch`.
  - Probe (.NET 10 `Regex`, no `IgnoreCase`, file `<scratch>/regexprobe2/probe2.cs`): frames pattern passes `120`, `-5`, `abc`, `0` and fails `120`+LF, `1 2`, `12`+U+212A, `1&2`, and the empty string. Time pattern passes `60`, `1` and fails `60`+LF, `0`, `abc`, `-3`, `5s`, and the empty string. 17 of 17 cases agree, `bad=0`.
  - ASCII check: no non-ASCII byte. CR count: 0. The help block needed no change.
- Task 2.6, the static checks (one shell command):
  - (a) `grep -nP '[^\x00-\x7F]'` on both scripts printed nothing. CR count: 0 for each script. PASS.
  - (b) `command -v pwsh` found nothing. The parse check and the five `pwsh` calls of task 2.6 (b) of `implementation.md` did not run: "not run". They are on the MANUAL list.
  - (c) Balanced characters: `test-windows.ps1` has 32 and 32 braces, 63 and 63 parentheses. `build.ps1` has 9 and 9 braces, 20 and 20 parentheses. No space after a line continuation backtick. No PowerShell 7 feature found (`$IsWindows`, `Kill($true)`, `-Environment`, `&&`, `||`, `??`, ternary).
  - (d) A search for `Set-ExecutionPolicy`, `devenv`, `msbuild.exe`, and `vswhere` found nothing in both scripts. `winget` appears only in the fix text (`$dotnetFix`, one hit in each script).
  - (e) Native commands: `& dotnet --version 2>$null` (the one allowed redirect, under `Continue`, in both scripts), `& dotnet build brutal-ui.sln` (no redirect), and the two `Start-Process $env:ComSpec` calls (the redirects `>nul 2>&1` and `> log 2>&1` are inside the `cmd.exe` string). No other PowerShell redirect or pipe follows a native command. PASS.
  - (f) The ASD-STE100 lint (`ste-lint.py --fail-over 2.5`): `build.ps1` 1.51 and `test-windows.ps1` 1.27 findings for each 100 words. PASS.
- Blast radius: the phase changes `scripts/test-windows.ps1` and adds `scripts/build.ps1`. No file under `src/`, `tests/`, or `samples/` changes.

### Phase 3 (2026-10-16)

- Task 3.1: wrote `docs/testing-windows.md` with the ten sections of the plan: Purpose, One-time setup, The procedure, The look checklist, The report block, Exit codes, Settings, Failures and fixes, Build only, Limits. The setup table uses the columns Item, Check, and Fix, and holds the rows Git, clone, .NET 10 SDK, script policy, and Vulkan loader. The note under the table names the `-ExecutionPolicy Bypass -File` form. The report block asks for the `Ctrl+C` exit code. The failure table holds one row for each `FAIL at` header of `test-windows.ps1` (six preflight rows, `build`, `test`, `loader`, the time-out, exit 2, exit 1 with `DllNotFoundException`, exit 1 with another error, and `interrupted`).
- Task 3.2: the sections "System Dependencies", "Native Dependencies", and "Related" below hold the Windows items, the no-native-file statement, the task 1.8 result, and the link to the two open items of the sibling.
- Task 3.3, the verification (Linux, no `pwsh`):
  - Step 1: `.gitattributes` non-comment lines are `* text=auto` and `*.sh text eol=lf`. `git check-attr` gives `eol: lf` for both `.sh` files. PASS (F1, F2).
  - Step 2: `git ls-files --eol | grep -E '^i/(crlf|mixed)'` printed nothing. The renormalize in the scratch clone staged no file. PASS (F3).
  - Step 3: the ASCII grep on both scripts and `.gitattributes` printed nothing. Both scripts hold `#Requires -Version 5.1`, `Set-StrictMode -Version Latest`, `$ErrorActionPreference = 'Stop'`, and a `.SYNOPSIS` help block. PASS (Q1, Q2).
  - Step 4: `command -v pwsh` found nothing. "Not run, MANUAL". The parse check stays on the MANUAL list.
  - Step 5: needs `pwsh`. "Not run, MANUAL". The check stays on the MANUAL list.
  - Step 6: the three contract strings (`dotnet build brutal-ui.sln`, `dotnet test --solution brutal-ui.sln`, `dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build -- --frames`) are in `test-windows.ps1`. `build.ps1` holds `dotnet build brutal-ui.sln`. The scripts read `BRUTAL_UI_FRAMES` and `BRUTAL_UI_SMOKE_TIMEOUT`, and use the prefix `test-windows:` and `artifacts\test-windows`. No ` -c `, no `dotnet test brutal-ui.sln`, no `Set-ExecutionPolicy`, `devenv`, `msbuild.exe`, or `vswhere`. `winget` appears only in `$dotnetFix` (one hit in each script). The native calls were read in phase 2 and did not change. PASS (I1, I2, Q4, Q5).
  - Step 7: `env -u DISPLAY -u WAYLAND_DISPLAY scripts/test-linux.sh` printed `test-linux: PASS build, test, gpu, smoke (120 frames) in 10 s` and `exit=0`. PASS (Q9).
  - Step 8: `git status --porcelain` shows no `artifacts`, `bin`, or `obj` path (Q6). `git diff --name-only` lists `docs/features/dev-ops/human-testing.md` and `implementation.md` of this feature. Both changes existed before phase 3. No change to `scripts/test-linux.sh`, `scripts/check-gpu.sh`, `docs/testing-linux.md`, or a file under `src/`, `tests/`, or `samples/`. PASS (I6).
  - Step 9: compared each command and each output string of the runbook with the two scripts: the PASS line, the failure header, the time-out suffix, the check lines, the six preflight problem texts and fixes, the loader text and fix, the `build:` lines, the two variable names and defaults, and the log paths. All agree. `ste-lint.py --fail-over 2.5` on the runbook: 0.61 findings for each 100 words, exit 0. PASS (F13, I3, I4, I5, Q8).
- Script defects found: none new. The three low findings of the phase 2 verifier stay open (see Session Notes, 2026-10-16 13:53). The runbook follows the scripts.
- Blast radius: the phase adds `docs/testing-windows.md` and edits this feature folder only. No script, source, test, or sample file changes.

### Review (2026-10-16)

- CR-1, CR-2, CR-3 (group 1, `scripts/test-windows.ps1` only): the main block sets `$scriptDone = $false` and `$stopCode = 1` before `try`, and `$scriptDone = $true` after the PASS line. The outer `finally` block sets `$global:LASTEXITCODE = $stopCode` when `$scriptDone` is false, with no cmdlet and no read of `$global:LASTEXITCODE`. An `exit <n>` sets the code again after that block, so `Write-CheckFail`, a failed step, and 124 keep their codes. The interrupted branch of `Invoke-Step` now sets `$script:stopCode = 130` and writes the interrupted line first, and then calls `Stop-StepTree`. After the wait, a step exit code of -1073741510 (`STATUS_CONTROL_C_EXIT`) with no time-out gives the interrupted line and `exit 130`, not the failure block. `Stop-StepTree` starts `cmd.exe` with a `ProcessStartInfo` (`UseShellExecute = $false`, `CreateNoWindow = $true`) and `Process.Start`, in place of `Start-Process`, because a stopped pipeline blocks the output object of a cmdlet. The `taskkill.exe` string and the 30 s and 10 s waits do not change. The help text gives 130 for `Ctrl+C`. Known limits: an error in the `try` block of `Invoke-Step` (for example a failed `Process.Start` in the time-out path) also gives the interrupted line and 130. A stop that PowerShell throws between `$waitDone = $true` and the -1073741510 check gives no line and exit code 1. A Windows run (F12) must confirm the exit code of the step after `Ctrl+C`.
- CR-6 (group 4, `context.md` only): the frames gotcha now names the pattern of the script (`-cnotmatch '^[0-9A-Za-z+-]+\z'`) and the effect of `\z` and the case-sensitive match. The two stale follow-up items (tasks 2.4 and 2.1) are marked done. The task 1.8 note now records the `DllImportSearchPath.System32` search of `Brutal.Vulkan`. The F12 line expects the interrupted line and `$LASTEXITCODE` 130, and asks for the full output after `Ctrl+C`.
- CR-5 (group 2, help block of `scripts/test-windows.ps1`, comment lines only): the help text now says that `Ctrl+C` during the build, test, or smoke step gives the interrupted line, because only `Invoke-Step` writes it. The `preflight` and `loader` steps give none. Preflight item 6 now reads "the script can delete the folder artifacts\test-windows" (active voice). Group 3 changes the runbook rows.
- CR-4 and the runbook side of CR-1, CR-2, CR-5 (group 3, `docs/testing-windows.md` only): the failure table now holds two Vulkan loader rows. Row 1 names `GLFW finds no Vulkan loader`, the exact text of `samples/HelloPanel/Program.cs` line 150. Row 2 names `Unable to load DLL 'vulkan-1'` with the inner exception of the `TypeInitializationException`. The `Brutal.Vulkan` library loads `vulkan-1` from `System32` only, so the fix of row 2 points to the `Test-Path` check of the setup table. The exit code table gives 130 for `Ctrl+C` in the build, test, or smoke step. The report block has a new optional line for the full output after `Ctrl+C`. Each changed string matches `scripts/test-windows.ps1`. No script changed.

---

## Gotchas & Learnings

- In a PowerShell string, `"$Step: text"` is a parse error, because PowerShell reads `$Step:` as a drive-qualified variable. The script uses `"${Step}: text"`.
- `cmd.exe` expands `%NAME%` also inside double quotes. The quotes around the frames value stop `&`, `|`, `<`, and `>`, but not `%`. The frames check of task 2.1 (`-cnotmatch '^[0-9A-Za-z+-]+\z'`) closes this gap. The `\z` anchor matches only at the true end of the string, so a trailing line feed fails (the `$` anchor lets it pass). The `-c` match is case-sensitive, so the Kelvin sign U+212A fails.
- Phase 1 converts `BRUTAL_UI_SMOKE_TIMEOUT` with `[int]` and no check. A bad value (`abc`) stops the script with a PowerShell error record and exit 1. `[int]'1.5'` rounds to 2. Task 2.1 adds the check before the conversion.
- Windows PowerShell 5.1 reads a log with no BOM as ANSI text. `dotnet` can write UTF-8. So a non-ASCII character in the tail of 30 lines can show as the wrong character. The full log keeps the bytes of `dotnet`.
- `cmd.exe` cannot use a UNC path as its working folder. A clone on a network share (`\\server\share`) can fail. The runbook of phase 3 can name a local clone.

---

## Testing Approach

### Static Tests (agent, on Linux)
- The ASCII check, the search for the contract strings, and a read check of each native call.
- The `.gitattributes` checks: `git check-attr`, `git ls-files --eol`, and a renormalize in a scratch clone.
- The Linux regression run of `scripts/test-linux.sh`.

### Integration Tests (MANUAL, on Windows)
- The script run itself: build, unit tests, loader check, and the Vulkan smoke run in a visible window.
- Failure injection: `BRUTAL_UI_FRAMES=0`, `BRUTAL_UI_SMOKE_TIMEOUT=5`, the two preflight values, and one broken source file.

---

## Needs Human Verification

`/autorun-feature` adds one line per item that the verifier cannot check, for a person to test.

- [ ] Phase 1 demo: the author runs `.\scripts\test-windows.ps1` in a PowerShell window at the repository root on Windows, sees the `HelloPanel` window open and close, then sees one line that starts with `test-windows: PASS`, and `$LASTEXITCODE` is 0.
- [ ] MANUAL (phase 1, Q3): `pwsh` does not exist on the Linux machine, so the parse check did not run. In a PowerShell window at the root of the clone, run `$e = $null; [void][System.Management.Automation.Language.Parser]::ParseFile("$PWD\scripts\test-windows.ps1", [ref]$null, [ref]$e); $e.Count`. Expect `0`.
- [ ] MANUAL (phase 1, F4): Run `cmd /c "powershell -NoProfile -ExecutionPolicy Bypass -File scripts\test-windows.ps1 >%TEMP%\out.txt 2>%TEMP%\err.txt"; $LASTEXITCODE; Get-Content $env:TEMP\out.txt; (Get-Item $env:TEMP\err.txt).Length`. Expect `0`, one line `test-windows: PASS build, test, smoke (120 frames) in <N> s`, and `0`. The `HelloPanel` window opens and closes.
- [ ] MANUAL (phase 1, F5 and Q7): Run `$env:DOTNET_NOLOGO; $env:MSBUILDDISABLENODEREUSE`. Then run `Set-Location $env:TEMP; & "<clone>\scripts\test-windows.ps1"; $LASTEXITCODE; Get-Location`. Then run the two variables again. Expect the PASS line, `0`, the `TEMP` folder, and the same variable values as before the call. Return to the clone.
- [ ] MANUAL (phase 1, F6): Run `$env:BRUTAL_UI_FRAMES = '0'; .\scripts\test-windows.ps1; $LASTEXITCODE; Remove-Item Env:BRUTAL_UI_FRAMES`. Expect exit `2`. Standard error starts with `test-windows: FAIL at smoke (exit 2)`, holds a `Usage:` line, and ends with `test-windows: full log: artifacts\test-windows\smoke.log`.
- [ ] MANUAL (phase 1, F7): Run `$env:BRUTAL_UI_FRAMES = '100000000'; $env:BRUTAL_UI_SMOKE_TIMEOUT = '5'; .\scripts\test-windows.ps1; $LASTEXITCODE; Get-Process HelloPanel -ErrorAction SilentlyContinue; Remove-Item Env:BRUTAL_UI_FRAMES, Env:BRUTAL_UI_SMOKE_TIMEOUT`. Expect exit `124` and a header with `timed out after 5 s`. `Get-Process` prints nothing.
- [ ] MANUAL (phase 1, F8): Write `src\BrutalUi.Core\Broken.cs` with the text `class Broken {`. Run `.\scripts\test-windows.ps1; $LASTEXITCODE`. Expect a non-zero exit code, `test-windows: FAIL at build`, and a `CS` error in the tail. Delete `Broken.cs`. Then `git status --porcelain` shows no change.
- [ ] MANUAL (phase 1, F9): Run `.\scripts\test-windows.ps1` two times in a row. Expect the PASS line from both runs. The second run deletes and writes `artifacts\test-windows` with no error. Then `git status --porcelain` shows no `artifacts` path (Q6).
- [ ] Phase 2 demo: the author runs `.\scripts\build.ps1` in a PowerShell window at the repository root on Windows, sees the `dotnet build` output end with `Build succeeded`, and `$LASTEXITCODE` is 0.
- [ ] MANUAL (phase 2, Q3): `pwsh` does not exist on the Linux machine, so the parse check of `build.ps1` did not run. Run `$e = $null; [void][System.Management.Automation.Language.Parser]::ParseFile("$PWD\scripts\build.ps1", [ref]$null, [ref]$e); $e.Count`. Expect `0`. Run the same call for `scripts\test-windows.ps1` again, because phase 2 changed it. Expect `0`.
- [ ] MANUAL (phase 2, F10): Run `.\scripts\build.ps1; $LASTEXITCODE`. Expect the `dotnet build` output, an end with `Build succeeded`, and `0`. Then write the temporary `src\BrutalUi.Core\Broken.cs` of F8 and run it again. Expect a non-zero exit code and the last line `build: FAIL at build (exit 1)`. Delete `Broken.cs`.
- [ ] MANUAL (phase 2, F11): Run `$env:BRUTAL_UI_SMOKE_TIMEOUT = 'abc'; .\scripts\test-windows.ps1; $LASTEXITCODE; Remove-Item Env:BRUTAL_UI_SMOKE_TIMEOUT`. Expect `test-windows: FAIL at preflight: BRUTAL_UI_SMOKE_TIMEOUT abc is not a whole number above 0`, the fix line, and `1`. Then run `$env:BRUTAL_UI_FRAMES = '1 2'; .\scripts\test-windows.ps1; $LASTEXITCODE; Remove-Item Env:BRUTAL_UI_FRAMES`. Expect the frames check lines and `1`.
- [ ] MANUAL (phase 2, F12): Run `$env:BRUTAL_UI_FRAMES = '100000000'; .\scripts\test-windows.ps1`. Press `Ctrl+C` while the window shows. Then run `$LASTEXITCODE; Get-Process HelloPanel -ErrorAction SilentlyContinue; Remove-Item Env:BRUTAL_UI_FRAMES`. Expect the line `test-windows: FAIL at smoke: interrupted`, `$LASTEXITCODE` 130, and no `HelloPanel` process. Copy the full output after `Ctrl+C` into the report block, so the Windows run confirms the exit code and the interrupted line. Then run `Get-Location`, and expect the folder of the call (the outer `finally` block runs `Pop-Location` after a stop).
- [ ] Phase 3 demo: the author opens `docs/testing-windows.md` on the Windows machine, follows its procedure from the pull to the report block, and finishes in under five minutes with the `test-windows: PASS` line and each item of the look checklist checked.
- [ ] MANUAL (phase 3, F14 and step 19): follow `docs/testing-windows.md` from the pull to the report block with a timer, on the second run. Expect under five minutes. Each item of the look checklist passes, and the sample exits 0 after the close from the taskbar. Send the report block to the agent.
- [ ] MANUAL (phase 2, Linux with `pwsh`): when a machine with `pwsh` exists, run `pwsh -NoProfile -File scripts/test-windows.ps1 >out.txt 2>err.txt; echo "exit=$?"`. Expect `test-windows: FAIL at preflight: this script runs on Windows only` in `err.txt`, an empty `out.txt`, `exit=1`, and no `artifacts/test-windows` folder. Run `BRUTAL_UI_SMOKE_TIMEOUT=abc pwsh -NoProfile -File scripts/test-windows.ps1; echo "exit=$?"` and `BRUTAL_UI_FRAMES='1 2' pwsh -NoProfile -File scripts/test-windows.ps1; echo "exit=$?"`. Expect the check lines and `exit=1` for each. Run `pwsh -NoProfile -File scripts/build.ps1; echo "exit=$?"`. Expect `exit=0`.

---

## System Dependencies

The scripts use these Windows items. The runbook `docs/testing-windows.md` holds the check and the fix for each one.

| Item | Used by | Source |
|------|---------|--------|
| Windows PowerShell 5.1 or PowerShell 7 | Both scripts (`#Requires -Version 5.1`) | Part of Windows. PowerShell 7 needs no install. |
| `cmd.exe` | `test-windows.ps1` runs each step as `cmd.exe /d /s /c` and writes each log | Part of Windows |
| `taskkill.exe` | `test-windows.ps1` stops the process tree on a time-out and on `Ctrl+C` | Part of Windows |
| .NET 10 SDK | Both scripts (`dotnet build`, `dotnet test`, `dotnet run`) | `winget install Microsoft.DotNet.SDK.10` |
| Git for Windows | The runbook (`git switch`, `git pull`, `git rev-parse`) | `winget install Git.Git` |
| `vulkan-1.dll` | The `loader` step and the `HelloPanel` smoke run | The GPU driver installs it in `System32` |

## Native Dependencies

- This feature adds no native file to the repository. `glfw3.dll` and `imgui.dll` come from the NuGet packages. `vulkan-1.dll` comes from the GPU driver. See the table in `docs/features/dev-ops/solution-skeleton/context.md`, section "Native Dependencies".
- Task 1.8 (2026-10-16): `Brutal.Vulkan` loads the Vulkan loader as `vulkan-1` on Windows. Source: `samples/HelloPanel/bin/Debug/net10.0/Brutal.Vulkan.dll` (the existing build, no rebuild). `strings -el` (the UTF-16 user strings of the code) shows `Vulkan`, `vulkan-1`, `VulkanEx`, `vulkan`, then `Failed to load Vulkan library` and `vkGetInstanceProcAddr`, in that order. `strings -a` shows `SetDllImportResolver`, `NativeLibrary`, `IsWindows`, `IsLinux`, and `IsMacOS` in the metadata. The sibling `solution-skeleton/context.md` also records the `SetDllImportResolver` call in a static constructor. A later read of the decompiled source (`<scratch>/decomp/Brutal.Vulkan/Brutal.VulkanApi/Vulkan.cs`, lines 281 to 305) gives the exact rule. The static constructor of `Brutal.VulkanApi.Vulkan` calls `NativeLibrary.Load("vulkan-1", typeof(Vulkan).Assembly, DllImportSearchPath.System32)` on Windows. So the library comes from `System32` only, and a `vulkan-1.dll` on `PATH` is not used. The resolver then maps the name `vulkan` to that handle. On Linux the call is `NativeLibrary.Load("vulkan", ..., DllImportSearchPath.LegacyBehavior)` (`libvulkan.so`). No `.dll` or `libvulkan` file name is in the assembly, so .NET adds the suffix. `ilspycmd` does not exist, so the agent did not read the IL. The Windows smoke run is the final check. The `loader` step of task 2.3 checks `vulkan-1.dll` in `[Environment]::SystemDirectory`, which agrees with this result.

---

## Next Steps

### Immediate (Today/This Session)
1. The caller (`/autorun-epic`) commits the work and runs `/update-epic dev-ops` and `/update-master`.
2. The author follows `docs/testing-windows.md` on the Windows machine and sends the report block.
3. The author runs the other MANUAL items of `## Needs Human Verification` (F5 to F12, the parse checks).

### Short Term (This Week)
1. Read the report block of the author. Fix each Windows defect that it shows.
2. After a PASS on Windows, the author checks "Phase 2 Windows" and "CR-1 Windows" of `dev-ops/solution-skeleton` in `docs/features/dev-ops/human-testing.md`.

---

## Open Questions

- none. Task 1.8 found the Windows mapping to `vulkan-1`, so no loader question is open.

### Follow-up Work and Migration Risks (phase 1)

- [x] Task 2.4: wrap the `while` loop of `Invoke-Step` in `try` and `finally`. Call `Stop-StepTree` in the `finally` block only when `$p.HasExited` is false. Write the interrupted line there. Done in phase 2. The review (CR-3) later moved the interrupted line before `Stop-StepTree`.
- [x] Task 2.1: move `[int]$smokeTimeoutText` after check 3. The check must refuse a value that `[int]` rounds (`1.5`) or that has spaces. Done in phase 2 (`[int]::TryParse` after the check).
- Risk: no agent can parse or run the script. The first Windows run is the first real test of `Start-Process -NoNewWindow -PassThru` with the handle read, the exit code pass-through of `cmd.exe /d /s /c`, and the tree kill.
- Risk: `taskkill.exe /T` follows the parent process ID. When `dotnet run` ends before `HelloPanel`, the tree link breaks and `HelloPanel` can stay alive. F7 checks it.

---

## Session Notes

### 2026-10-16 13:33
- Started the feature from `/autorun-feature` in the called mode of `/autorun-epic`.
- `pwsh` does not exist on this machine. No agent can run a Windows check.

### 2026-10-16 13:40
- Phase 1 done: tasks 1.1 to 1.9 (brief `phase-1.md`, advanced agent). The agent added a helper `Stop-StepTree` for the `taskkill.exe` call, so task 2.4 can reuse it. It waits up to 30 s for `taskkill.exe`, then up to 10 s for the step.
- Verifier `phase-1-verify.md`: PASS on 8 static checks (`.gitattributes` rules and renormalize, ASCII and CR, the help block and the header lines, the contract strings, the step runner rules, no PowerShell redirect of a native command, no PowerShell 7 feature, the `Push-Location` frame, the save and restore of the two variables). Demo: MANUAL (needs Windows and a person).
- Inbox: no open line.

### 2026-10-16 (phase 1)
- Wrote `.gitattributes` and `scripts/test-windows.ps1`. Ran the checks of tasks 1.2 and 1.9. Found the loader name `vulkan-1` in task 1.8. No build ran, because the DLL exists.

### 2026-10-16 13:53
- Phase 2 done: tasks 2.1 to 2.7 (brief `phase-2.md`, standard agent). The agent added a `$waitDone` flag in `Invoke-Step`. The preflight runs inside the `try` block, so a failed check also restores the two variables.
- Verifier `phase-2-verify.md`, run 1: FAIL. Gate 4: both preflight value checks used `-match` with `$`, so a value with a trailing LF passed, and the frames check passed the Kelvin sign U+212A. The verifier also named a risk: `Ctrl+C` can end the step processes before the `finally` block runs, so the interrupted line did not appear.
- Fix 1 (`phase-2-fix1.md`): both checks use `-cnotmatch` with `\z`. The `finally` block of `Invoke-Step` writes the interrupted line after each stop of the wait loop, and stops the tree only when the process still runs. A .NET probe passed 17 of 17 cases.
- Verifier `phase-2-verify.md`, run 2: PASS on 10 checks. `dotnet build brutal-ui.sln`: 0 warnings, 0 errors. Demo: MANUAL (needs Windows and a person).
- Low findings of run 2, for the cross-phase review: the help line "Ctrl+C during a step" also covers `preflight` and `loader`, which give no interrupted line (`scripts/test-windows.ps1:35`). A throw in `Stop-StepTree` hides the interrupted line (lines 140 to 143). The gotcha in this file still names the old frames pattern.
- Inbox: 1 bug line, done (`phase-2-fix1.md`).

### 2026-10-16 13:57
- Phase 3 done: tasks 3.1 to 3.3 (brief `phase-3.md`, standard agent). `docs/testing-windows.md` holds the ten sections. Lint 0.61 per 100 words. `scripts/test-linux.sh` still prints its PASS line (10 s).
- Verifier `phase-3-verify.md`: PASS on Verification Method steps 1 to 9 (steps 4 and 5 not run, `pwsh` does not exist, MANUAL lines exist). Each command and output string of the runbook matches the scripts. Demo: MANUAL (needs Windows, a GPU, a desktop, and a timer).
- Low findings for the cross-phase review: the runbook line 58 names `human-testing.md` as the source of the two open sibling items, and `context.md` names the sibling `context.md` (both hold the items). The runbook line 148 names the .NET message `Unable to load DLL 'vulkan'` or `vulkan-1`, which nobody has seen. The `Brutal.Vulkan` text is `Failed to load Vulkan library`.
- Inbox: no open line.

### 2026-10-16 14:20
- Cross-phase review (brief `review.md`, advanced agent): `code-review.md` holds 6 findings (2 MEDIUM, 4 LOW). The Vercel React rules of `/review-feature` do not apply to PowerShell, so the review used Windows PowerShell 5.1 semantics, the plan quality bar, the output contract, `AGENTS.md`, and ASD-STE100.
- Fixed: CR-1 (exit code 130 after `Ctrl+C`, 1 after an error), CR-2 (`STATUS_CONTROL_C_EXIT` step code as an interrupt), CR-3 (the interrupted line before the tree stop, `Process.Start` in `Stop-StepTree`), CR-4 (the runbook Vulkan rows), CR-5 (the help text), CR-6 (stale `context.md` lines). Briefs `review-g1.md` (advanced) to `review-g4.md`.
- Rejected known finding: the runbook names `human-testing.md` as the source of the two open sibling items. That file holds both items, and plan Notes 5 names it.
- Verifier `review-verify.md`: PASS on 9 checks. Build 0 warnings, 0 errors. `scripts/test-linux.sh` PASS (9 s). Its two low notes (the status line of `code-review.md` and the stale "Immediate" list) are fixed in this update. Its info note on `Pop-Location` after a stop is now part of the F12 line.
- Next feature draft: none (the last feature of the epic).
- Inbox bug: Verifier FAIL: phase 2 gate 4, the preflight regex checks of `scripts/test-windows.ps1` (lines 216 and 221) pass a value that ends with a LF, and the frames check also passes the Kelvin sign U+212A. (done: phase-2-fix1.md)
- Inbox: 1 line, done. No open line. No bug to promote.
- `implementation.md` status: Ready for Review.

---

*Update this file at the end of each work session. Run `/update-feature` before compacting conversations.*
