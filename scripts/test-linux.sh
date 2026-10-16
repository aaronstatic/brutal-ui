#!/usr/bin/env bash
# test-linux.sh - Build, test, and smoke-run brutal-ui on Linux with no display.
#
# Purpose:
#   Prove the full path on a headless machine. The script runs the build, the
#   unit tests, and the GPU driver check. Then it runs the HelloPanel sample on
#   the NVIDIA GPU, inside Xvfb.
#
# Steps (the first failure stops the run):
#   0. preflight - check that dotnet, xvfb-run, and timeout exist, that dotnet
#                  is version 10, and that BRUTAL_UI_SMOKE_TIMEOUT is valid
#   1. build     - dotnet build brutal-ui.sln
#   2. test      - dotnet test --solution brutal-ui.sln
#   3. gpu       - scripts/check-gpu.sh (after the tests, so a driver problem
#                  does not hide the unit test result)
#   4. loader    - check that libvulkan.so exists next to the libvulkan.so.1 of ldconfig
#   5. smoke     - run HelloPanel for a number of frames inside xvfb-run
#
# Output:
#   Success: one line on standard output, and nothing on standard error.
#   Failure: the failure block on standard error. Each step writes its full
#   log to artifacts/test-linux/<step>.log. The `preflight` and `loader` steps
#   have no log. They write the problem and the fix to standard error.
#
# Exit codes:
#   0     all steps passed
#   1     a check failed (preflight, gpu, loader), or HelloPanel failed at run time
#   2     HelloPanel reported a usage error
#   124   the smoke step timed out
#   137   the smoke step timed out and timeout had to kill it
#   130   SIGINT (Ctrl+C) stopped the smoke step
#   143   SIGTERM stopped the smoke step
#   other the exit code of dotnet build or dotnet test
#
# Environment variables:
#   BRUTAL_UI_FRAMES          number of frames for the smoke run (default 120)
#   BRUTAL_UI_SMOKE_TIMEOUT   time limit of the smoke step in seconds, a whole
#                             number above 0 (default 60)

set -euo pipefail

CDPATH= cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.."

FRAMES="${BRUTAL_UI_FRAMES:-120}"
SMOKE_TIMEOUT="${BRUTAL_UI_SMOKE_TIMEOUT:-60}"
LOG_DIR=artifacts/test-linux

export DOTNET_NOLOGO=1

rm -rf -- "$LOG_DIR"
mkdir -p -- "$LOG_DIR"

# fail <step> <problem> <fix>
# Write the two lines of a failed check to standard error. Exit with code 1.
fail() {
    printf 'test-linux: FAIL at %s: %s\n' "$1" "$2" >&2
    printf 'test-linux: fix: %s\n' "$3" >&2
    exit 1
}

# The PID of the smoke step, and the name of a signal that stopped it.
step_pid=
caught_signal=

# stop_step <signal>
# The trap handler of the smoke step. Record the signal. Send TERM to the step.
stop_step() {
    caught_signal=$1
    if [ -n "$step_pid" ]; then
        kill -TERM "$step_pid" 2>/dev/null || true
    fi
}

# run_step <name> <command...>
# Run the command. Send its output to LOG_DIR/<name>.log. On a non-zero exit
# code, write the failure block to standard error and exit with the same code.
# After SIGINT or SIGTERM in the smoke step, write the interrupted line to
# standard error and exit with 130 or 143.
run_step() {
    local name=$1
    shift
    local log="$LOG_DIR/$name.log"
    local rc=0
    if [ "$name" = smoke ]; then
        # timeout moves itself and its child into a new process group. Ctrl+C
        # then does not reach it, and a SIGTERM to this script leaves Xvfb
        # alive. So the step runs in the background, and the trap sends TERM
        # to timeout. timeout then sends TERM to its full process group.
        trap 'stop_step INT' INT
        trap 'stop_step TERM' TERM
        "$@" >"$log" 2>&1 &
        step_pid=$!
        if [ -n "$caught_signal" ]; then
            kill -TERM "$step_pid" 2>/dev/null || true
        fi
        wait "$step_pid" || rc=$?
        # wait returns above 128 when the trap fires. Wait again until the
        # step really ends.
        while [ -n "$caught_signal" ] && kill -0 "$step_pid" 2>/dev/null; do
            wait "$step_pid" || true
        done
        trap - INT TERM
        step_pid=
        if [ -n "$caught_signal" ]; then
            printf 'test-linux: FAIL at %s: interrupted\n' "$name" >&2
            if [ "$caught_signal" = INT ]; then
                exit 130
            fi
            exit 143
        fi
    else
        "$@" >"$log" 2>&1 || rc=$?
    fi
    if [ "$rc" -ne 0 ]; then
        local header="test-linux: FAIL at $name (exit $rc)"
        if [ "$name" = smoke ] && { [ "$rc" -eq 124 ] || [ "$rc" -eq 137 ]; }; then
            header="$header, timed out after $SMOKE_TIMEOUT s"
        fi
        {
            printf '%s\n' "$header"
            tail -n 30 -- "$log"
            printf 'test-linux: full log: %s\n' "$log"
        } >&2
        exit "$rc"
    fi
}

# Preflight: a missing tool gives a clear fix in place of a later error.
command -v dotnet >/dev/null 2>&1 ||
    fail preflight "dotnet does not exist" "sudo apt install dotnet-sdk-10.0"
command -v xvfb-run >/dev/null 2>&1 ||
    fail preflight "xvfb-run does not exist" "sudo apt install xvfb"
command -v timeout >/dev/null 2>&1 ||
    fail preflight "timeout does not exist" "sudo apt install coreutils"
dotnet_version=$(dotnet --version 2>/dev/null || true)
if [[ $dotnet_version != 10.* ]]; then
    fail preflight "dotnet version ${dotnet_version:-unknown} is not version 10" \
        "sudo apt install dotnet-sdk-10.0"
fi
# A bad value fails here, not after the build. The value 0 turns off the time
# limit of timeout, so the check refuses it.
[[ $SMOKE_TIMEOUT =~ ^[1-9][0-9]*$ ]] ||
    fail preflight "BRUTAL_UI_SMOKE_TIMEOUT $SMOKE_TIMEOUT is not a whole number above 0" \
        "Set BRUTAL_UI_SMOKE_TIMEOUT to a number of seconds, for example 60."

run_step build dotnet build brutal-ui.sln

run_step test dotnet test --solution brutal-ui.sln

# The GPU check runs after the tests, so a driver problem does not hide the
# unit test result.
run_step gpu scripts/check-gpu.sh

# Brutal.Vulkan loads the name "vulkan", so .NET probes the unversioned
# libvulkan.so. Only the package libvulkan-dev installs that file. The package
# libvulkan1 installs libvulkan.so.1 only. See
# docs/features/dev-ops/solution-skeleton/context.md, section "Native
# Dependencies". This script creates no link and sets no LD_LIBRARY_PATH.
if command -v ldconfig >/dev/null 2>&1; then
    ldconfig_cmd=ldconfig
else
    ldconfig_cmd=/sbin/ldconfig
fi
ldconfig_list=$("$ldconfig_cmd" -p 2>/dev/null || true)
# ldconfig -p lists only the SONAME libvulkan.so.1, never the unversioned
# development link. So find the folder of the x86-64 libvulkan.so.1 entry, and
# test for libvulkan.so in that folder. The exact name match skips the drivers
# libvulkan_lvp.so, libvulkan_intel.so, and the other libvulkan_* files.
vulkan_entry='^[[:space:]]*libvulkan\.so\.1[[:space:]]+\([^)]*x86-64[^)]*\)[[:space:]]+=>[[:space:]]+(.+)$'
vulkan_path=
while IFS= read -r line; do
    if [[ $line =~ $vulkan_entry ]]; then
        vulkan_path=${BASH_REMATCH[1]}
        break
    fi
done <<<"$ldconfig_list"
if [ -z "$vulkan_path" ] || [ ! -e "${vulkan_path%/*}/libvulkan.so" ]; then
    fail loader \
        "the unversioned Vulkan loader libvulkan.so does not exist" \
        "sudo apt install libvulkan-dev"
fi

# The smoke run reads the exit code of HelloPanel, not its output.
# The literal ICD path must match the default CHECK_GPU_ICD_FILE of
# scripts/check-gpu.sh.
run_step smoke \
    env -u WAYLAND_DISPLAY -u VK_DRIVER_FILES -u VK_ADD_DRIVER_FILES \
    XDG_SESSION_TYPE=x11 \
    VK_ICD_FILENAMES=/usr/share/vulkan/icd.d/nvidia_icd.json \
    timeout --kill-after=10 "$SMOKE_TIMEOUT" \
    xvfb-run -a dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build -- --frames "$FRAMES"

printf 'test-linux: PASS build, test, gpu, smoke (%s frames) in %s s\n' "$FRAMES" "$SECONDS"
