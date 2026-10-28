# Windows Testing Runbook

## 1. Purpose

This runbook shows how to build, test, and smoke-run brutal-ui on a Windows machine with a GPU. One script does the full path. It runs the build, the unit tests, and the Vulkan loader check. Then it runs the `HelloPanel` sample in a visible window.

## 2. One-time setup

Run the check first. Run the fix only when the check fails. The scripts run in Windows PowerShell 5.1 and in PowerShell 7. Open a PowerShell window for each command of this runbook.

| Item | Check | Fix |
|------|-------|-----|
| Git | `git --version` | `winget install Git.Git` |
| A clone of the repository | `git -C <clone> status` | `git clone <URL-OF-THE-AUTHOR> <clone>` (use a local folder, not a network share) |
| .NET 10 SDK | `dotnet --version` prints a version that starts with `10.` | `winget install Microsoft.DotNet.SDK.10` |
| Script policy | `Get-ExecutionPolicy -Scope CurrentUser` prints `RemoteSigned`, `Unrestricted`, or `Bypass` | `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned` |
| Vulkan loader | `Test-Path "$env:SystemRoot\System32\vulkan-1.dll"` prints `True` | Install or update the GPU driver. The driver installs `vulkan-1.dll`. |

Notes for the table:

- Clone: replace `<URL-OF-THE-AUTHOR>` with the URL of the repository. Run each later command at the root of the clone.
- Script policy: a file from `git clone` has no download mark, so `RemoteSigned` runs it.
- Script policy: when the policy cannot change, use this call in place of `.\scripts\test-windows.ps1`: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\test-windows.ps1`
- .NET 10 SDK: open a new PowerShell window after the install, so the window reads the new path.
- Vulkan loader: `Brutal.Vulkan` loads `vulkan-1`, so .NET looks for `vulkan-1.dll`.

## 3. The procedure

The first run downloads the packages and can take longer than five minutes. The five-minute target counts from the second run.

1. Pull the newest `main`:

   ```
   git switch main
   git pull --ff-only
   ```

2. Run the test script from the root of the clone:

   ```
   .\scripts\test-windows.ps1
   ```

   The `HelloPanel` window opens for about two seconds. Do not touch the window, because `smoke.log` holds the real frame count. The script prints one line that starts with `test-windows: PASS`. Run `$LASTEXITCODE` to see `0`.

3. Look at the sample:

   ```
   dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build
   ```

   The test script already built the Debug output, so `--no-build` works. Do the checklist of section 4.

4. Report the result. Fill the block of section 5.

## 4. The look checklist

Do each item in this order. Each item states one action and one result. This list covers the open items "Phase 2 Windows" and "CR-1 Windows" of `docs/features/dev-ops/human-testing.md`.

1. The window title is `HelloPanel`.
2. The window shows the text `Hello, brutal-ui` on a dark background.
3. Click `Click me` two times. The label next to it reads `Button clicks: 2`.
4. Move the pointer over `Click me`. The fill gets lighter. The panel loses its hover fill while the pointer is over a widget. This is correct.
5. Press Tab until `Click me` has the focus. Its border turns yellow.
6. Click the gray `Disabled` button. No hover fill shows, and no label changes.
7. Click the small square of the toggle. The square turns green, and its label reads `Toggle: on`.
8. Drag the thumb of the slider to the right. The label reads `Slider value: ` with a number that follows the thumb.
9. Press Tab until the slider has the focus, then press Right. The value goes up by 1.
10. Turn the mouse wheel down over the list. The items move up, and the label reads `Scroll offset: 40 px`.
11. Drag the list with the left button. The items follow the pointer.
12. Click the text field and type `Hello`. The label reads `Text: Hello`, and the caret sits right after the `o`, with a gap of 1 pixel or less.
13. Press Enter in the field. The second label reads `Submitted: Hello`.
14. Type letters until the text is wider than the field. The text moves left, and the caret stays inside the field.
15. Click an empty part of the root, then press Tab five times. The focus visits the panel, `Click me`, the toggle, the slider, and the text field, each with a yellow border. The disabled button and the list take no focus.
16. Minimize the window.
17. Restore the window. See that the window draws again.
18. Minimize the window again.
19. Close the window from the taskbar. Right-click the taskbar button, then click Close window.
20. Run `$LASTEXITCODE`. It prints `0`.
21. Run `Get-Process HelloPanel -ErrorAction SilentlyContinue`. It prints nothing.

## 5. The report block

Copy this block. Fill each line. Paste it into the agent session, or into the `inbox.md` of the active feature.

```
date: <YYYY-MM-DD>
commit: <output of git rev-parse --short HEAD>
dotnet: <output of dotnet --version>
test script: <the PASS line, or the FAIL lines>
1 title is HelloPanel: <yes or no>
2 text Hello, brutal-ui on a dark background: <yes or no>
3 Click me gives Button clicks: 2: <yes or no>
4 hover fill on Click me: <yes or no>
5 yellow focus border on Click me after Tab: <yes or no>
6 the Disabled button gives no change: <yes or no>
7 toggle turns green and reads Toggle: on: <yes or no>
8 slider drag, the label follows the thumb: <yes or no>
9 slider Right key adds 1: <yes or no>
10 wheel gives Scroll offset: 40 px: <yes or no>
11 the drag moves the list: <yes or no>
12 text field shows Text: Hello, caret gap of 1 pixel or less: <yes or no, and the gap>
13 Enter gives Submitted: Hello: <yes or no>
14 long text scrolls under the caret: <yes or no>
15 Tab visits panel, button, toggle, slider, field, and skips the disabled button and the list: <yes or no>
16-17 minimize and restore, the window draws again: <yes or no>
18-19 minimize again and close from the taskbar: <yes or no>
20 LASTEXITCODE after the close is 0: <yes or no, and the value>
21 no HelloPanel process: <yes or no>
time from the pull to this block (second run): <minutes>
ctrl+c exit code (optional): <the value of $LASTEXITCODE after Ctrl+C>
ctrl+c full output (optional): <copy the full output after Ctrl+C, with the line test-windows: FAIL at <step>: interrupted>
```

After the report, the author checks the items "Phase 2 Windows" and "CR-1 Windows" in `docs/features/dev-ops/human-testing.md`.

## 6. Exit codes

| Case | Standard output | Standard error | Exit code |
|------|-----------------|----------------|-----------|
| Success | One line: `test-windows: PASS build, test, smoke (<FRAMES> frames) in <S> s` | Nothing | 0 |
| A step fails | Nothing | `test-windows: FAIL at <step> (exit <rc>)`, then the last 30 lines of `artifacts\test-windows\<step>.log`, then `test-windows: full log: artifacts\test-windows\<step>.log` | `<rc>` of the step |
| A smoke time-out | Nothing | The header adds `, timed out after <SMOKE_TIMEOUT> s` | 124 |
| A check fails (`preflight`, `loader`) | Nothing | `test-windows: FAIL at <step>: <problem>`, then `test-windows: fix: <command>` | 1 |
| `Ctrl+C` in the build, test, or smoke step | Nothing | `test-windows: FAIL at <step>: interrupted` | 130 |

The exit code of `build` or `test` is the exit code of `dotnet`. `HelloPanel` exits with 1 for a runtime failure and with 2 for a usage error. The exit code 130 means that `Ctrl+C` stopped the build, test, or smoke step. A Windows run has not yet confirmed the value. Record the real `$LASTEXITCODE` after `Ctrl+C` in the report block.

Each step writes its full log to the folder `artifacts\test-windows\`. The file name is `<step>.log`. The `preflight` and `loader` steps have no log. The script deletes the folder at the start of each run. Git ignores the folder.

The steps run in this order. The first failure stops the run.

1. `preflight` checks the tools, the two variables, and the operating system.
2. `build` runs `dotnet build brutal-ui.sln`.
3. `test` runs `dotnet test --solution brutal-ui.sln`.
4. `loader` checks that `vulkan-1.dll` exists in the system folder.
5. `smoke` runs `dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build -- --frames "<FRAMES>"`, with a time limit.

## 7. Settings

Set these environment variables before the call.

| Variable | Meaning | Default |
|----------|---------|---------|
| `BRUTAL_UI_FRAMES` | The number of frames of the smoke run | 120 |
| `BRUTAL_UI_SMOKE_TIMEOUT` | The time limit of the smoke step in seconds. Use a whole number above 0. | 60 |

An empty variable counts as unset. Example:

```
$env:BRUTAL_UI_FRAMES = '300'
.\scripts\test-windows.ps1
Remove-Item Env:BRUTAL_UI_FRAMES
```

A variable stays set in the PowerShell session until you remove it. Run `Remove-Item Env:<name>` after each test.

## 8. Failures and fixes

Read the header line first. It names the step. Then read the tail of the log, and do the fix.

| Header | Read first | Fix |
|--------|------------|-----|
| `FAIL at preflight: dotnet does not exist` | The `fix:` line | `winget install Microsoft.DotNet.SDK.10`. Open a new PowerShell window. |
| `FAIL at preflight: dotnet version ... is not version 10` | The version in the header | `winget install Microsoft.DotNet.SDK.10`. Remove any other SDK from your path. |
| `FAIL at preflight: BRUTAL_UI_SMOKE_TIMEOUT ... is not a whole number above 0` | The value in the header | Set `BRUTAL_UI_SMOKE_TIMEOUT` to a number of seconds, for example 60. |
| `FAIL at preflight: BRUTAL_UI_FRAMES ... holds a character that is not a letter, a digit, a plus sign, or a minus sign` | The value in the header | Set `BRUTAL_UI_FRAMES` to a whole number above 0, for example 120. |
| `FAIL at preflight: this script runs on Windows only` | The `fix:` line | Run `scripts/test-linux.sh` on Linux. |
| `FAIL at preflight: artifacts\test-windows is in use` | The `fix:` line | Close each `HelloPanel` window. Run `dotnet build-server shutdown`. Run the script again. |
| `FAIL at build (exit <rc>)` | The `error` lines with a `CS` code in the tail | Fix the C# error. Run the script again. |
| `FAIL at test (exit <rc>)` | The failed test names in the tail | Fix the test or the code. Read `artifacts\test-windows\test.log` for the full list. |
| `FAIL at loader: the Vulkan loader vulkan-1.dll does not exist in ...` | The `fix:` line | Install or update the GPU driver. The driver installs `vulkan-1.dll`. |
| `FAIL at smoke (exit 124), timed out after <N> s` | The end of `artifacts\test-windows\smoke.log` | Find the frame where the run stops. Raise `BRUTAL_UI_SMOKE_TIMEOUT` only when the run is slow and not stuck. |
| `FAIL at smoke (exit 2)` | The `Usage:` line in the log | Set `BRUTAL_UI_FRAMES` to a whole number above 0. |
| `FAIL at smoke (exit 1)` with `GLFW finds no Vulkan loader` | The line that holds `GLFW finds no Vulkan loader` in `artifacts\test-windows\smoke.log` | Install or update the GPU driver. |
| `FAIL at smoke (exit 1)` with `Unable to load DLL 'vulkan-1'` | The inner exception of the `TypeInitializationException` in `artifacts\test-windows\smoke.log` | The driver must install `vulkan-1.dll` in `System32`. Run the `Test-Path` check of the setup table in section 2. |
| `FAIL at smoke (exit 1)` with `allocated <B> bytes after warm-up` | The line `HelloPanel: Update and Draw allocated <B> bytes after warm-up. The limit is 0 bytes.` in `artifacts\test-windows\smoke.log` | A handler or a draw path of a widget allocates. Tell the agent, and paste the line. |
| `FAIL at smoke (exit 1)` with `did not reach each widget` | Line 5 of `artifacts\test-windows\smoke.log`: the count that is 0 | The synthetic input missed the widget of that count. Tell the agent, and paste line 5. |
| `FAIL at smoke (exit 1)` with another error | The exception in `artifacts\test-windows\smoke.log` | Update the GPU driver. Then read the exception. |
| `FAIL at <step>: interrupted` | Nothing. You pressed `Ctrl+C` in the build, test, or smoke step. | Run `Get-Process HelloPanel -ErrorAction SilentlyContinue`. It must print nothing. Run the script again. |

After a time-out, run `Get-Process HelloPanel -ErrorAction SilentlyContinue`. It must print nothing. When it prints a process, run `Stop-Process -Name HelloPanel` and tell the agent.

## 9. Build only

Run this call to build with no test and no window:

```
.\scripts\build.ps1
```

| Case | Console | Exit code |
|------|---------|-----------|
| Success | The `dotnet build` output only | 0 |
| The build fails | The `dotnet build` output, then `build: FAIL at build (exit <rc>)` on standard error | `<rc>` of `dotnet` |
| A check fails | `build: FAIL at preflight: <problem>`, then `build: fix: <command>` on standard error | 1 |

The script checks only that `dotnet` exists and is version 10.

## 10. Limits

- No CI run. The script runs on the machine of the author only.
- No Visual Studio. The scripts need the .NET 10 SDK and nothing else.
- No installer. The scripts install nothing. The fix lines name the `winget` commands.
- No screenshot. The smoke run reads the exit code of `HelloPanel`, not its image. The author does the look.
- The Debug configuration only. The scripts use no `-c` option.
- One run at a time. The script deletes `artifacts\test-windows\` at the start of each run.
