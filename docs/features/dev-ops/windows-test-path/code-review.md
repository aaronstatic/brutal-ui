# dev-ops/windows-test-path - Code Review

**Reviewed:** 2026-10-16
**Reviewer:** Agent (cross-phase review)
**Rules Applied:** Windows PowerShell 5.1 and PowerShell 7 semantics, the plan quality bar, the output contract, `AGENTS.md`, ASD-STE100. The Vercel React rules do not apply to PowerShell.
**Status:** Resolved - 6 findings, 6 resolved (verifier `review-verify.md` PASS on 2026-10-16)

---

## Summary

- HIGH: 0
- MEDIUM: 2 (CR-1, CR-2). Both are on the `Ctrl+C` path. Both make the MANUAL check F12 fail, so each costs a round trip through the author.
- LOW: 4 (CR-3 to CR-6). Four of the five known low findings of the verifiers are confirmed. One is rejected (see the result of the review).

The success path, the failure block, the time-out path, the preflight checks, and `build.ps1` show no defect in a read check against Windows PowerShell 5.1 and PowerShell 7 rules. No agent can run the scripts (`pwsh` does not exist on this machine), so each finding below is a read check. CR-2 and CR-3 depend on internal rules of the PowerShell engine for a stopped pipeline. The text of each finding names the part that a Windows run must confirm.

## Findings

### CR-1: A stop with no `exit` leaves `$LASTEXITCODE` at 0 in the caller session
**Severity:** MEDIUM
**Rule:** 1 (correctness on Windows PowerShell 5.1 and PowerShell 7), 2 (one exit code tells the truth)
**File:** `scripts/test-windows.ps1:38, 47, 133-145, 269-274`
**Problem:** `$LASTEXITCODE` is a global variable. Each native command sets it. The script runs one native command in PowerShell: `& dotnet --version 2>$null` (line 205). After that call, `$LASTEXITCODE` is 0 in the caller session. `Start-Process` does not set it. Only `exit <n>` sets it again. Two paths end the script with no `exit`:
- `Ctrl+C`. PowerShell stops the pipeline. The `finally` blocks run (lines 133-145 and 269-274), but no statement sets an exit code. The interactive call `.\scripts\test-windows.ps1` of the runbook then leaves `$LASTEXITCODE` at 0.
- An unexpected terminating error, for example `New-Item` (line 246), `Start-Process`, or `Get-Content` with `$ErrorActionPreference = 'Stop'`. The script stops with an error record, and `$LASTEXITCODE` stays 0.

So the help text is false for the interactive call:
```
      PowerShell sets a non-zero exit code after the interrupt.      (line 38)
      1      ... or the script stopped on an error                   (line 42-43)
      other  non-zero code that PowerShell sets after Ctrl+C         (line 47)
```
F12 expects "a non-zero `$LASTEXITCODE`" after `Ctrl+C`. The author sees 0. The runbook rows `docs/testing-windows.md:98` and `:100` repeat the false text. The plan note 3 ("PowerShell sets the exit code after a stop") is the source. Only the `powershell -File` form gives a non-zero process exit code after an error (1). Its code after `Ctrl+C` is not known.
**Fix:**
1. In the main block, set `$scriptDone = $false` and `$stopCode = 1` before `try`, and `$scriptDone = $true` after the PASS line. In the outer `finally` block, add `if (-not $scriptDone) { $global:LASTEXITCODE = $stopCode }`. An `exit <n>` sets the code again after the `finally` block, so the exit paths do not change. Use only variable assignments here, and no cmdlet, because the pipeline is stopped. Do not read `$global:LASTEXITCODE`, because it does not exist in a new session and `Set-StrictMode` stops on it.
2. In the `finally` block of `Invoke-Step`, in the interrupted branch, set `$script:stopCode = 130` as the first statement. The value mirrors `test-linux.sh` (130 for `SIGINT`).
3. Change the help lines 38 and 47 and the exit code table to "130 Ctrl+C stopped a step". Change `docs/testing-windows.md:98` and `:100` to the same code, and keep the F12 request to record the real value.
**Effort:** Low

### CR-2: A `Ctrl+C` that ends the step inside the wait gives the failure block, not the interrupted line
**Severity:** MEDIUM
**Rule:** 1 (correctness), 3 (the output contract row "`Ctrl+C` in a step")
**File:** `scripts/test-windows.ps1:122-132`
**Problem:** The step runs with `-NoNewWindow`, so `cmd.exe`, `dotnet`, and `HelloPanel` share the console of PowerShell. `Ctrl+C` reaches each of them at the same time as PowerShell. The fix 1 comment (lines 136-138) knows this. But the flag `$waitDone` then gives a wrong result:
```powershell
        while (-not $p.WaitForExit(200)) { ... }
        $waitDone = $true
```
The loop spends almost all of its time inside `WaitForExit(200)`, a .NET call that PowerShell cannot stop. When the step processes end inside the remaining part of those 200 ms, `WaitForExit` returns `true`, the loop ends normally, and `$waitDone` becomes `true`. The PowerShell engine checks for a stop at each loop iteration. No confirmed check occurs at the plain assignment. Then the `finally` block writes no interrupted line. The script reads `$p.ExitCode`, probably -1073741510 (`0xC000013A`, `STATUS_CONTROL_C_EXIT`, which `dotnet run` and `cmd.exe /c` pass through), and writes `test-windows: FAIL at smoke (exit -1073741510)`. The next cmdlet (`Test-Path`, line 158) can then throw the pending stop. In that case the tail, the log line, and the `exit` do not occur, and CR-1 applies. A `HelloPanel` with a short shutdown makes this case common for the `smoke` step. F12 then fails. The result changes from run to run.
**Fix:** After the loop and before the `rc` check, treat the code of a console interrupt as an interrupt: when `-not $timedOut -and $p.ExitCode -eq -1073741510`, set `$global:LASTEXITCODE = 130`, write `test-windows: FAIL at ${Name}: interrupted`, and `exit 130`. Keep the `finally` branch for a stop that occurs while the step runs. Add one line to F12: record the full output after `Ctrl+C`, so the Windows run confirms the exit code of the step.
**Effort:** Low

### CR-3: The interrupted line comes after `Stop-StepTree`, so a throw in that call hides the line
**Severity:** LOW
**Rule:** 1 (correctness), 3 (the output contract)
**File:** `scripts/test-windows.ps1:139-143` (with `Stop-StepTree`, lines 85-95)
**Problem:** The `finally` block calls `Stop-StepTree` before it writes the line:
```powershell
            if (-not $p.HasExited) {
                Stop-StepTree -Process $p
            }
            [Console]::Error.WriteLine("test-windows: FAIL at ${Name}: interrupted")
```
`Stop-StepTree` runs `Start-Process -PassThru`. This `finally` block runs in a stopped pipeline. PowerShell does not let a cmdlet write to the output stream of a stopped pipeline, so the `-PassThru` object can throw `PipelineStoppedException` or arrive as `$null`. A `$null` gives an error at `$kill.Handle` under `Set-StrictMode -Version Latest`. Either way the exception leaves the `finally` block before the line. This branch runs when the step process still runs after `Ctrl+C`. That is the usual case for the `build` and `test` steps, because MSBuild cancels the build over a few seconds. The `taskkill.exe` process can start before the throw, so the risk is a lost line, not an orphan.
**Fix:** Write the interrupted line (and the exit code of CR-1) first, then stop the tree. In `Stop-StepTree`, start `taskkill.exe` through `cmd.exe` with a `[System.Diagnostics.ProcessStartInfo]` (`UseShellExecute = $false`, `CreateNoWindow = $true`) and `[System.Diagnostics.Process]::Start`, in place of `Start-Process`. A .NET call does not use the output stream, so a stopped pipeline does not block it. The `cmd.exe /d /s /c "taskkill.exe ... >nul 2>&1"` string stays the same.
**Effort:** Low

### CR-4: The runbook names an error text that does not occur on Windows for a missing Vulkan loader
**Severity:** LOW
**Rule:** 3 (the output contract and the runbook)
**File:** `docs/testing-windows.md:148`
**Problem:** The row reads:
```
| `FAIL at smoke (exit 1)` with `DllNotFoundException` for `vulkan` | The `Unable to load DLL 'vulkan'` or `vulkan-1` line in the log | ...
```
Two facts make this text wrong on Windows:
- `HelloPanel` calls `Glfw.VulkanSupported()` before any `Brutal.Vulkan` call (`samples/HelloPanel/Program.cs:147-151`). GLFW loads `vulkan-1.dll` itself. When the file does not exist, the log holds `HelloPanel: System.InvalidOperationException: GLFW finds no Vulkan loader: ...`. No `DllNotFoundException` occurs.
- The static constructor of `Brutal.VulkanApi.Vulkan` calls `NativeLibrary.Load("vulkan-1", typeof(Vulkan).Assembly, DllImportSearchPath.System32)` on Windows (decompiled source of the earlier session: `<scratch>/decomp/Brutal.Vulkan/Brutal.VulkanApi/Vulkan.cs:281-305`). The name `vulkan` is the Linux name only. A failure gives a `TypeInitializationException` for `Brutal.VulkanApi.Vulkan` with an inner `DllNotFoundException: Unable to load DLL 'vulkan-1' or one of its dependencies`. This occurs only when GLFW finds a `vulkan-1.dll` outside `System32` (for example on `PATH`), because `Brutal.Vulkan` searches `System32` only. The text `Failed to load Vulkan library` (the verifier note) cannot occur on Windows, because `NativeLibrary.Load` throws before that check.

So the author does not find the named line, and goes to the next row.
**Fix:** Replace the row with two rows. (1) `FAIL at smoke (exit 1)` with `GLFW finds no Vulkan loader`: read that line. Fix: install or update the GPU driver. (2) `FAIL at smoke (exit 1)` with `Unable to load DLL 'vulkan-1'`: read the inner exception of the `TypeInitializationException`. Fix: the driver must install `vulkan-1.dll` in `System32`. Run the `Test-Path` check of the setup table.
**Effort:** Low

### CR-5: The help text says that `Ctrl+C` during any step gives the interrupted line
**Severity:** LOW
**Rule:** 3 (the output contract), 4 (ASD-STE100)
**File:** `scripts/test-windows.ps1:26, 35-37`
**Problem:** The help block lists five steps, with `preflight` and `loader`. Then it says:
```
      Interrupt: Ctrl+C during a step stops the step and its child processes.
      The script then writes this line to standard error:
      test-windows: FAIL at <step>: interrupted
```
Only `Invoke-Step` writes the line, so only `build`, `test`, and `smoke` give it. `Ctrl+C` during `preflight` (for example during `dotnet --version` or the folder delete) or during `loader` gives no line. Line 26 also uses the passive voice ("the folder artifacts\test-windows can be deleted"), which ASD-STE100 does not allow. The runbook rows `docs/testing-windows.md:98` and `:150` use the same `<step>` text.
**Fix:** Change line 35 to "Ctrl+C during the build, test, or smoke step stops the step and its child processes." Change line 26 to "6. the script can delete the folder artifacts\test-windows". Change the runbook row 98 to "`Ctrl+C` in the build, test, or smoke step".
**Effort:** Low

### CR-6: `context.md` holds stale statements after the fix and the later phases
**Severity:** LOW
**Rule:** 5 (maintainability), `AGENTS.md` (keep the dev docs current)
**File:** `docs/features/dev-ops/windows-test-path/context.md:119, 195-200`
**Problem:**
- Line 119 (Gotchas & Learnings) says that the frames check `^[0-9A-Za-z+-]+$` closes the `%` gap. The script uses `-cnotmatch '^[0-9A-Za-z+-]+\z'` (line 227). The `$` anchor of the old pattern lets a trailing line feed through, which is the defect that fix 1 corrected. A reader who copies the gotcha copies the defect.
- Lines 195-198 (Follow-up Work, phase 1) list tasks 2.4 and 2.1 as open work. Phase 2 did both.
- Line 178 (Native Dependencies) says that "the resolver gives `vulkan-1` on Windows". The decompiled source shows a direct `NativeLibrary.Load("vulkan-1", ..., DllImportSearchPath.System32)` in the static constructor, and the resolver maps `vulkan` to that handle. The result (`vulkan-1.dll`) is correct. The `System32`-only search is a fact that the `loader` step depends on, and the file does not record it.
**Fix:** Change the pattern on line 119 to `-cnotmatch '^[0-9A-Za-z+-]+\z'` and add one sentence about the `\z` anchor and the case-sensitive match. Mark the two follow-up items done, or delete them. Add the `DllImportSearchPath.System32` fact to the task 1.8 note.
**Effort:** Low

## What's Already Good

- The step runner follows the section "The Step Runner" exactly: one `Start-Process` string for `cmd.exe /d /s /c`, the redirect inside `cmd.exe`, the handle read, the 200 ms wait loop, the `Stopwatch` limit, and the `taskkill.exe /T /F` call with `>nul 2>&1`. The `/s` quote rule gives the exact command for all three steps, also with the inner quotes of `--frames "<FRAMES>"`.
- The only PowerShell redirect of a native command is `& dotnet --version 2>$null`, under `Continue`, with `Stop` restored in a `finally` block. So the Windows PowerShell 5.1 error record trap cannot occur.
- `exit` in `Write-CheckFail` and `Invoke-Step` runs inside the outer `try`, so each failure restores `DOTNET_NOLOGO`, `MSBUILDDISABLENODEREUSE`, and the current folder. `[Environment]::SetEnvironmentVariable` with a `$null` old value removes the variable, so an unset variable stays unset.
- `Push-Location` and relative paths keep the session folder unchanged. All paths go through the PowerShell location or the explicit `-WorkingDirectory`, never through the process folder.
- The preflight checks use `-cnotmatch` with `\z` and ASCII ranges (`[0-9]`, not `\d`), so a trailing line feed, a Unicode digit, and the Kelvin sign all fail. `[int]::TryParse` refuses a value over `[int]::MaxValue`.
- The loader step tests `vulkan-1.dll` in `[Environment]::SystemDirectory`. This matches the `DllImportSearchPath.System32` search of `Brutal.Vulkan` exactly.
- The handle stays open for the full step, so `taskkill.exe /PID` cannot hit a new process with the same ID.
- `build.ps1` is short, holds no Windows-only call, and passes the exit code of `dotnet build` through.
- Each output string of the runbook matches the scripts. The ASCII check passes, and `ste-lint.py --fail-over 2.5` passes on the runbook (0.61), `test-windows.ps1` (1.30), and `build.ps1` (1.51).
- `.gitattributes` holds the two planned rules and changes no tracked file.

## Recommended Fix Order

1. CR-1, CR-2, and CR-3 together. They change the same 25 lines of `Invoke-Step` and the outer `finally` block. Do them before the author runs F12, so one Windows run checks the final `Ctrl+C` path.
2. CR-5, with the help block edit of CR-1 and the runbook row 98.
3. CR-4.
4. CR-6.

## Progress

- [x] CR-1: A stop with no `exit` leaves `$LASTEXITCODE` at 0 in the caller session
- [x] CR-2: A `Ctrl+C` that ends the step inside the wait gives the failure block, not the interrupted line
- [x] CR-3: The interrupted line comes after `Stop-StepTree`, so a throw in that call hides the line
- [x] CR-4: The runbook names an error text that does not occur on Windows for a missing Vulkan loader
- [x] CR-5: The help text says that `Ctrl+C` during any step gives the interrupted line
- [x] CR-6: `context.md` holds stale statements after the fix and the later phases
