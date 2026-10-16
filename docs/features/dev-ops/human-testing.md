# Dev Ops — Human Testing

**Epic:** dev-ops
**Last Updated:** 2026-10-16 13:40

> An `/autorun-epic` run adds one section per feature, and one demo line per slice as the slice lands. Check an item after you test it.

## solution-skeleton

- [ ] Phase 1 demo: the user runs `dotnet test --solution brutal-ui.sln` at the repository root and sees 3 passed tests and exit code 0.
- [ ] Phase 2 demo: the user runs `dotnet run --project samples/HelloPanel/HelloPanel.csproj -- --frames 120`, sees a window with the text `Hello, brutal-ui`, and the process exits with code 0.
- [ ] Phase 3 demo: the user runs `dotnet pack brutal-ui.sln -c Release` and sees exactly five `.nupkg` files in `artifacts/packages/`.
- [ ] Phase 2 Linux: make `libvulkan.so` available first, with `libvulkan-dev` or a link on `LD_LIBRARY_PATH`. Without it, the sample exits 1 with `DllNotFoundException` for `vulkan`.
- [ ] Phase 2 Windows: do the Phase 2 demo run on Windows. Nobody ran the sample on Windows yet.
- [ ] CR-1 Windows: run `HelloPanel` with no argument. Minimize the window, restore it, and then minimize it again. Close it from the taskbar. Expect exit code 0.

## linux-dev-environment

- [ ] Phase 1 demo: the agent runs `env -u DISPLAY scripts/test-linux.sh` at the repository root, sees one line that starts with `test-linux: PASS`, and the exit code is 0.
- [ ] Phase 2 demo: the agent runs `scripts/check-gpu.sh`, sees the kernel module version, the user space library version, the Vulkan ICD, and `check-gpu: OK`, and the exit code is 0.
- [ ] Phase 3 demo: the author opens `docs/testing-linux.md`, runs each check command of its one-time setup table, runs the one script call that it names, and sees `test-linux: PASS`.
- [ ] Desktop run (I5): run `scripts/test-linux.sh` in a terminal of the desktop session. Expect the PASS line and no window on the screen. The agents ran the script only with no `DISPLAY`.

## windows-test-path

- [ ] Phase 1 demo: the author runs `.\scripts\test-windows.ps1` in a PowerShell window at the repository root on Windows, sees the `HelloPanel` window open and close, then sees one line that starts with `test-windows: PASS`, and `$LASTEXITCODE` is 0.
- [ ] Phase 2 demo: the author runs `.\scripts\build.ps1` in a PowerShell window at the repository root on Windows, sees the `dotnet build` output end with `Build succeeded`, and `$LASTEXITCODE` is 0.
- [ ] Phase 3 demo: the author opens `docs/testing-windows.md` on the Windows machine, follows its procedure from the pull to the report block, and finishes in under five minutes with the `test-windows: PASS` line and each item of the look checklist checked.
- [ ] Order: do the Phase 3 demo first, with a timer, on the second run. It also covers "Phase 2 Windows" and "CR-1 Windows" of `solution-skeleton`.
- [ ] Parse check (Q3): on Windows, run the `Parser]::ParseFile` call from `context.md` for both scripts. Expect `0` for each.
- [ ] F5 and Q7: on Windows, call the script by its full path from `$env:TEMP`. Expect the PASS line and the same folder after the call. `DOTNET_NOLOGO` and `MSBUILDDISABLENODEREUSE` keep their old values.
- [ ] F6: on Windows, set `$env:BRUTAL_UI_FRAMES = '0'` and run the script. Expect exit 2, `FAIL at smoke (exit 2)`, a `Usage:` line, and the log path last.
- [ ] F7: on Windows, set `BRUTAL_UI_FRAMES` to `100000000` and `BRUTAL_UI_SMOKE_TIMEOUT` to `5`. Expect exit 124, `timed out after 5 s`, and no `HelloPanel` process after the run.
- [ ] F8 and F10: on Windows, add a temporary `src\BrutalUi.Core\Broken.cs` with `class Broken {`. Expect `FAIL at build` and a `CS` error from `test-windows.ps1`, and `build: FAIL at build (exit 1)` from `build.ps1`. Delete the file after the check.
- [ ] F9: on Windows, run the script two times in a row. Expect the PASS line from both runs, and no `artifacts` path in `git status --porcelain`.
- [ ] F11: on Windows, set `BRUTAL_UI_SMOKE_TIMEOUT` to `abc`, then set `BRUTAL_UI_FRAMES` to `1 2`. Expect the preflight problem line, the fix line, and exit 1 for each.
- [ ] F12: on Windows, press `Ctrl+C` while the smoke window shows. Expect `test-windows: FAIL at smoke: interrupted`, `$LASTEXITCODE` 130, no `HelloPanel` process, and the folder of the call. Copy the full output into the report block.
