# dev-ops/windows-test-path - Task Checklist

**Last Updated:** 2026-10-16 14:20
**Progress:** 19/19 tasks complete (100%)
**Advanced phases:** Phase 1 (ADVANCED, opus). Phases 2 and 3 are STANDARD (sonnet).

---

## Phase 1: The Windows Test Script Runs Build, Test, and the Smoke Run (9/9 complete)

**Demo:** the author runs `.\scripts\test-windows.ps1` in a PowerShell window at the repository root on Windows, sees the `HelloPanel` window open and close, then sees one line that starts with `test-windows: PASS`, and `$LASTEXITCODE` is 0.
**Needs:** none

- [x] ✅ **1.1 Write `.gitattributes`**
  - Description: Write `.gitattributes` at the repository root with the exact text of the Architecture Overview: three comment lines, `* text=auto`, and `*.sh text eol=lf`. LF line endings and ASCII text only.
  - File(s): `.gitattributes`
  - Estimate: 3 steps

- [x] ✅ **1.2 Check `.gitattributes`**
  - Description: In one shell command, run checks (a) to (e) of task 1.2 of `implementation.md`: `git ls-files --eol` shows no `i/crlf` or `i/mixed`. A renormalize in a scratchpad clone stages no file (never run `git add --renormalize` in the real repository). `git check-attr text eol` gives `eol: lf` for both `.sh` files and `text: auto` with `eol: unspecified` for the `.sln`. `grep -c $'\r' scripts/*.sh` gives 0. `git status --porcelain` shows no unexpected modified tracked file. Record each result in `context.md`.
  - File(s): `docs/features/dev-ops/windows-test-path/context.md`
  - Estimate: 6 steps

- [x] ✅ **1.3 Write the frame of `scripts\test-windows.ps1`**
  - Description: ASCII only. Comment-based help block in ASD-STE100 prose (purpose, steps, output, exit codes, the two variables). `#Requires -Version 5.1`, `Set-StrictMode -Version Latest`, `$ErrorActionPreference = 'Stop'`. No `$PSNativeCommandUseErrorActionPreference`. `$root = Split-Path -Parent $PSScriptRoot`. Body inside `Push-Location -LiteralPath $root` and `try { ... } finally { Pop-Location }`. `$frames` from `BRUTAL_UI_FRAMES` (default `120`), `$smokeTimeout` from `BRUTAL_UI_SMOKE_TIMEOUT` (default `60`, as `[int]`). `$logDir = 'artifacts\test-windows'`. Save, set to `1`, and restore `DOTNET_NOLOGO` and `MSBUILDDISABLENODEREUSE`. Delete and create `$logDir` each run. Start a `Stopwatch`.
  - File(s): `scripts/test-windows.ps1`
  - Estimate: 8 steps

- [x] ✅ **1.4 Write the helper `Write-CheckFail`**
  - Description: `Write-CheckFail <step> <problem> <fix>` writes the two check lines of the output contract with `[Console]::Error.WriteLine` and calls `exit 1`.
  - File(s): `scripts/test-windows.ps1`
  - Estimate: 3 steps

- [x] ✅ **1.5 Write the helper `Invoke-Step`**
  - Description: `Invoke-Step <Name> <Command> [<TimeoutSeconds>]` per the section "The Step Runner": `Start-Process $env:ComSpec` with `/d /s /c "<command> > artifacts\test-windows\<step>.log 2>&1"`, `-NoNewWindow -PassThru`, read `$p.Handle`, wait loop of `WaitForExit(200)` with a `Stopwatch`, tree kill with `taskkill.exe /PID <id> /T /F` through `cmd.exe` with `>nul 2>&1`. On a non-zero code: the failure block (header, `Get-Content -Tail 30`, log path) to standard error, then `exit` with the code. Smoke time-out: exit 124 and `, timed out after <N> s` in the header.
  - File(s): `scripts/test-windows.ps1`
  - Estimate: 10 steps

- [x] ✅ **1.6 Add the build and test steps**
  - Description: `Invoke-Step build 'dotnet build brutal-ui.sln'` and `Invoke-Step test 'dotnet test --solution brutal-ui.sln'`. No `-c`. No positional `dotnet test brutal-ui.sln`.
  - File(s): `scripts/test-windows.ps1`
  - Estimate: 2 steps

- [x] ✅ **1.7 Add the smoke step and the PASS line**
  - Description: `Invoke-Step smoke "dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build -- --frames `"$frames`"" $smokeTimeout`. Then exactly one line with `[Console]::Out.WriteLine`: `test-windows: PASS build, test, smoke (<FRAMES> frames) in <S> s`, with `<S>` from `[math]::Floor($sw.Elapsed.TotalSeconds)`.
  - File(s): `scripts/test-windows.ps1`
  - Estimate: 4 steps

- [x] ✅ **1.8 Find the Windows name of the Vulkan loader**
  - Description: Search `samples/HelloPanel/bin/Debug/net10.0/Brutal.Vulkan.dll` with `strings -a` and `strings -el` for `vulkan`. Use `ilspycmd` only when it exists. Install nothing. Record the result in `context.md`, section "Native Dependencies". When the package loads `vulkan` with no Windows mapping to `vulkan-1`, record the finding in "Open Questions" and report it as an inbox item. The script does not change.
  - File(s): `docs/features/dev-ops/windows-test-path/context.md`
  - Estimate: 6 steps

- [x] ✅ **1.9 Check statically on Linux**
  - Description: In one shell command: (a) the ASCII check of the script and `.gitattributes`. (b) `command -v pwsh`, then the parse check and `Invoke-ScriptAnalyzer` when they exist, else record "not run" and add the parse check to the MANUAL list. (c) Search for the contract strings of I1, ` -c `, and `dotnet test brutal-ui.sln`. (d) Confirm that no PowerShell redirect follows a native command. (e) Add the MANUAL checks of this phase to "Needs Human Verification" of `context.md`: the demo, F4 to F9, and Q7.
  - File(s): `docs/features/dev-ops/windows-test-path/context.md`
  - Estimate: 6 steps

---

## Phase 2: Preflight, Loader Check, Interrupt Cleanup, and the Build Script (7/7 complete)

**Demo:** the author runs `.\scripts\build.ps1` in a PowerShell window at the repository root on Windows, sees the `dotnet build` output end with `Build succeeded`, and `$LASTEXITCODE` is 0.
**Needs:** Phase 1

- [x] ✅ **2.1 Add the `preflight` step**
  - Description: Before the log folder reset, in this order: (1) `dotnet` exists, (2) `dotnet --version` starts with `10.` (`Continue` for this one call, `2>$null`), (3) `BRUTAL_UI_SMOKE_TIMEOUT` matches `^[1-9][0-9]*$` and passes `[int]::TryParse`, (4) `BRUTAL_UI_FRAMES` matches `^[0-9A-Za-z+-]+$`, (5) the OS is `Win32NT`. Exact problem and fix texts of task 2.1 of `implementation.md`. Move the `[int]` conversion after check 3.
  - File(s): `scripts/test-windows.ps1`
  - Estimate: 8 steps

- [x] ✅ **2.2 Make the log folder reset a check**
  - Description: Delete with `-ErrorAction Stop` in `try` and `catch`. On a failure, `Write-CheckFail preflight 'artifacts\test-windows is in use' 'Close each HelloPanel window. Run dotnet build-server shutdown. Run the script again.'`.
  - File(s): `scripts/test-windows.ps1`
  - Estimate: 3 steps

- [x] ✅ **2.3 Add the `loader` step**
  - Description: After `test`, before `smoke`. `Test-Path -LiteralPath (Join-Path ([Environment]::SystemDirectory) 'vulkan-1.dll')`. On a failure, `Write-CheckFail loader 'the Vulkan loader vulkan-1.dll does not exist in <folder>' 'Install or update the GPU driver. The driver installs vulkan-1.dll.'`. A comment names `docs/features/dev-ops/solution-skeleton/context.md`, section "Native Dependencies", and the result of task 1.8.
  - File(s): `scripts/test-windows.ps1`
  - Estimate: 4 steps

- [x] ✅ **2.4 Add the interrupt cleanup to `Invoke-Step`**
  - Description: The wait loop in `try` and `finally`. When `finally` runs and the step process still runs, stop the tree with `taskkill.exe` (task 1.5 pattern) and write `test-windows: FAIL at <step>: interrupted` to standard error. No PASS line after an interrupt. Do not catch the stop. Update the help block: the preflight checks, the `loader` step, the interrupt line, and the exit codes.
  - File(s): `scripts/test-windows.ps1`
  - Estimate: 6 steps

- [x] ✅ **2.5 Write `scripts\build.ps1`**
  - Description: ASCII only. Help block in ASD-STE100 prose, `#Requires -Version 5.1`, `Set-StrictMode -Version Latest`, `$ErrorActionPreference = 'Stop'`. The same `Push-Location` frame and the same save and restore of `DOTNET_NOLOGO`. No `MSBUILDDISABLENODEREUSE`. Checks 1 and 2 of task 2.1 with the prefix `build:`. `& dotnet build brutal-ui.sln` with no redirect. On a non-zero `$LASTEXITCODE`, `build: FAIL at build (exit <rc>)` to standard error. Exit with the code. No Windows-only call.
  - File(s): `scripts/build.ps1`
  - Estimate: 8 steps

- [x] ✅ **2.6 Check statically on Linux**
  - Description: (a) The ASCII check for both scripts. (b) When `pwsh` exists: the parse check for both scripts and the four `pwsh` calls of task 2.6 (b) of `implementation.md`. (c) When `pwsh` does not exist, record "not run" and add each check of (b) to the MANUAL list in PowerShell form. (d) Search both scripts for `Set-ExecutionPolicy`, `devenv`, `msbuild.exe`, and `vswhere`: no command. `winget` only inside fix text. Record each result in `context.md`.
  - File(s): `docs/features/dev-ops/windows-test-path/context.md`
  - Estimate: 6 steps

- [x] ✅ **2.7 Add the MANUAL checks of phase 2**
  - Description: Add to "Needs Human Verification" of `context.md`: the demo, F10 to F12, and the exit code that PowerShell gives after `Ctrl+C`. Ask the author to record that code in the report block.
  - File(s): `docs/features/dev-ops/windows-test-path/context.md`
  - Estimate: 2 steps

---

## Phase 3: The Windows Testing Runbook (3/3 complete)

**Demo:** the author opens `docs/testing-windows.md` on the Windows machine, follows its procedure from the pull to the report block, and finishes in under five minutes with the `test-windows: PASS` line and each item of the look checklist checked.
**Needs:** Phase 2

- [x] ✅ **3.1 Write `docs/testing-windows.md`**
  - Description: ASD-STE100 prose, with the ten sections of task 3.1 of `implementation.md`: Purpose, One-time setup (Item, Check, Fix), the procedure, the look checklist (title, text, minimize, restore, minimize, close from the taskbar, exit code, no `HelloPanel` process), the report block, Exit codes, Settings, Failures and fixes, Build only, Limits.
  - File(s): `docs/testing-windows.md`
  - Estimate: 10 steps

- [x] ✅ **3.2 Record the Windows dependencies in `context.md`**
  - Description: "System Dependencies" table (Windows PowerShell 5.1 or PowerShell 7, `cmd.exe`, `taskkill.exe`, the .NET 10 SDK, Git for Windows, `vulkan-1.dll`). "Native Dependencies": no native file enters the repository, with a link to the table of `docs/features/dev-ops/solution-skeleton/context.md` and the result of task 1.8. "Related": the look checklist covers "Phase 2 Windows" and "CR-1 Windows" of the sibling.
  - File(s): `docs/features/dev-ops/windows-test-path/context.md`
  - Estimate: 4 steps

- [x] ✅ **3.3 Verify the feature**
  - Description: Run each non-MANUAL step of the Verification Method. Compare each command and output string of the runbook with the two scripts, character for character. Run `ste-lint.py --fail-over 2.5` on the runbook and fix each finding over the limit. Run `scripts/test-linux.sh` one time. Record the results in `context.md`. Add the MANUAL steps and the phase demo to "Needs Human Verification".
  - File(s): `docs/features/dev-ops/windows-test-path/context.md`
  - Estimate: 8 steps

---

## Bugs & Issues

**Active Bugs:**
- none

**Fixed Bugs:**
- [x] ✅ 🐛 **Preflight value checks passed a trailing LF and the Kelvin sign** - Fixed 2026-10-16
  - Fix: `-cnotmatch` with `\z` for `BRUTAL_UI_SMOKE_TIMEOUT` and `BRUTAL_UI_FRAMES` (`phase-2-fix1.md`). The `finally` block of `Invoke-Step` writes the interrupted line after each stop.
- [x] ✅ 🐛 **Cross-phase review CR-1 to CR-6** - Fixed 2026-10-16
  - Fix: exit code 130 after `Ctrl+C` and 1 after an error, the `STATUS_CONTROL_C_EXIT` step code as an interrupt, the interrupted line before the tree stop, the help text, the runbook Vulkan rows, and the stale `context.md` lines (briefs `review-g1.md` to `review-g4.md`).

---

## Technical Debt

- none
