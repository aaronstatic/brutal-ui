# Linux Testing Runbook

## 1. Purpose

This runbook shows how to build, test, and smoke-run brutal-ui on a Linux machine with no display. One script does the full path. It runs the build, the unit tests, and the GPU driver check. Then it runs the `HelloPanel` sample on the NVIDIA GPU inside Xvfb.

## 2. One-time setup

Do each fix one time, with `sudo`. Run the check first. Run the fix only when the check fails.

| Item | Check | Fix |
|------|-------|-----|
| .NET 10 SDK | `dotnet --version` | `sudo apt install dotnet-sdk-10.0` |
| Xvfb | `command -v xvfb-run` | `sudo apt install xvfb` |
| Vulkan loader | `ls -l /usr/lib/x86_64-linux-gnu/libvulkan.so` (the path on Ubuntu x86-64) | `sudo apt install libvulkan-dev` |
| NVIDIA driver | `scripts/check-gpu.sh` | The fix that the script prints |

Notes for the table:

- .NET 10 SDK: Install the package from the Ubuntu archive. Do not mix it with the `packages.microsoft.com` feed. Two sources of the same SDK cause version conflicts.
- Vulkan loader: `ldconfig -p` lists only `libvulkan.so.1`. The `loader` step reads the folder of that entry and tests for `libvulkan.so` in it.
- NVIDIA driver: `scripts/check-gpu.sh` prints the kernel module version, the user space library version, and the Vulkan ICD. It prints `check-gpu: OK` when no problem exists.

`Brutal.Vulkan` loads the unversioned `libvulkan.so`, and only `libvulkan-dev` installs that file.

## 3. Run the tests

Run this call from any folder:

```
scripts/test-linux.sh
```

The script changes to the repository root by itself. Use the path of the script from your current folder.

On success, the script prints one line on standard output, and nothing on standard error:

```
test-linux: PASS build, test, gpu, smoke (<FRAMES> frames) in <S> s
```

Each step writes its full log to the folder `artifacts/test-linux/`. The file name is `<step>.log`. The `preflight` and `loader` steps have no log. They write the problem and the fix to standard error. The script deletes the folder at the start of each run. Git ignores the folder.

The steps run in this order. The first failure stops the run.

1. `preflight` checks that `dotnet`, `xvfb-run`, and `timeout` exist, that `dotnet` is version 10, and that `BRUTAL_UI_SMOKE_TIMEOUT` is a whole number above 0.
2. `build` runs `dotnet build brutal-ui.sln`.
3. `test` runs `dotnet test --solution brutal-ui.sln`.
4. `gpu` runs `scripts/check-gpu.sh`.
5. `loader` checks that `libvulkan.so` exists next to the `libvulkan.so.1` of `ldconfig -p`.
6. `smoke` runs `HelloPanel` for a number of frames inside `xvfb-run`.

## 4. Exit codes

| Case | Standard output | Standard error | Exit code |
|------|-----------------|----------------|-----------|
| Success | One line: `test-linux: PASS build, test, gpu, smoke (<FRAMES> frames) in <S> s` | Nothing | 0 |
| A step fails | Nothing | `test-linux: FAIL at <step> (exit <rc>)`, then the last 30 lines of `artifacts/test-linux/<step>.log`, then `test-linux: full log: artifacts/test-linux/<step>.log` | `<rc>` of the step |
| A smoke time-out | Nothing | The header adds `, timed out after <SMOKE_TIMEOUT> s` | 124 (137 when `timeout` must kill the run) |
| A check fails (`preflight`, `loader`) | Nothing | `test-linux: FAIL at <step>: <problem>`, then `test-linux: fix: <command>` | 1 |
| `Ctrl+C` or `SIGTERM` in the smoke step | Nothing | `test-linux: FAIL at smoke: interrupted` | 130 for `SIGINT`, 143 for `SIGTERM` |

The exit code of `build` or `test` is the exit code of `dotnet`. `HelloPanel` exits with 1 for a runtime failure and with 2 for a usage error.

## 5. Settings

Set these environment variables before the call.

| Variable | Meaning | Default |
|----------|---------|---------|
| `BRUTAL_UI_FRAMES` | The number of frames of the smoke run | 120 |
| `BRUTAL_UI_SMOKE_TIMEOUT` | The time limit of the smoke step in seconds. Use a whole number above 0. | 60 |

Example:

```
BRUTAL_UI_FRAMES=300 BRUTAL_UI_SMOKE_TIMEOUT=120 scripts/test-linux.sh
```

## 6. Failures and fixes

Read the header line first. It names the step. Then read the tail of the log, and do the fix.

| Header | Read first | Fix |
|--------|------------|-----|
| `FAIL at preflight: dotnet does not exist` | The `fix:` line | `sudo apt install dotnet-sdk-10.0` |
| `FAIL at preflight: xvfb-run does not exist` | The `fix:` line | `sudo apt install xvfb` |
| `FAIL at preflight: timeout does not exist` | The `fix:` line | `sudo apt install coreutils` |
| `FAIL at preflight: dotnet version ... is not version 10` | The version in the header | `sudo apt install dotnet-sdk-10.0`. Remove any other SDK source from your path. |
| `FAIL at preflight: BRUTAL_UI_SMOKE_TIMEOUT ... is not a whole number above 0` | The value in the header | `Set BRUTAL_UI_SMOKE_TIMEOUT to a number of seconds, for example 60.` |
| `FAIL at build (exit <rc>)` | The `error` lines with a `CS` code in the tail | Fix the C# error. Run the script again. |
| `FAIL at test (exit <rc>)` | The failed test names in the tail | Fix the test or the code. Read `artifacts/test-linux/test.log` for the full list. |
| `FAIL at gpu (exit 1)` | The `check-gpu: FAIL` lines in the tail | Do the `check-gpu: fix:` line. A kernel module and user space library mismatch needs a reboot. |
| `FAIL at loader: the unversioned Vulkan loader libvulkan.so does not exist` | The `fix:` line | `sudo apt install libvulkan-dev` |
| `FAIL at smoke (exit 124), timed out after <N> s` | The end of `artifacts/test-linux/smoke.log` | Find the frame where the run stops. Raise `BRUTAL_UI_SMOKE_TIMEOUT` only when the run is slow and not stuck. |
| `FAIL at smoke (exit 2)` | The `Usage:` line in the log | Set `BRUTAL_UI_FRAMES` to a whole number above 0. |
| `FAIL at smoke (exit 1)` with `DllNotFoundException` for `vulkan` | The `Unable to load shared library 'vulkan'` line in the log | `sudo apt install libvulkan-dev` |
| `FAIL at smoke (exit 1)` with another error | The exception in `artifacts/test-linux/smoke.log` | Run `scripts/check-gpu.sh`. Then read the exception. |

The GPU mismatch is the most common failure. A driver update changes the files on disk, but the kernel module in memory stays at the old version. A reboot loads the new kernel module.

## 7. Limits

- No CI run. The script runs on the machine of the author only.
- No Docker. The script needs the NVIDIA driver and the Vulkan ICD of the host.
- No OpenGL run. The smoke run uses Vulkan only.
- No pixel compare. The smoke run reads the exit code of `HelloPanel`, not its image.
- One run at a time. The script deletes `artifacts/test-linux/` at the start of each run.
