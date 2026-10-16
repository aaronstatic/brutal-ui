#!/usr/bin/env bash
# check-gpu.sh - Check that the NVIDIA driver is ready for a Vulkan run.
#
# Purpose:
#   Find a driver problem before a Vulkan error hides it. A driver update with
#   no reboot is the most common problem. The kernel module in memory and the
#   user space library on disk then have two versions.
#
# Items (the script checks every item, and then exits):
#   1. Kernel module     - the version in line 1 of the proc file
#   2. User space library - the version in the file name of libnvidia-ml.so.1
#   3. Vulkan ICD        - the ICD file, and its library_path and api_version
#   The script also compares the version of item 1 with the version of item 2.
#
# Output:
#   Standard output: one "check-gpu:" line for each item that the script reads,
#   and "check-gpu: OK" when no problem exists.
#   Standard error: for each problem, a "check-gpu: FAIL" line and a
#   "check-gpu: fix:" line.
#
# Exit codes:
#   0   no problem
#   1   at least one problem
#
# Test seams (environment variables that replace a source for a test):
#   CHECK_GPU_PROC_FILE   kernel module version file (default /proc/driver/nvidia/version)
#   CHECK_GPU_ICD_FILE    Vulkan ICD file (default /usr/share/vulkan/icd.d/nvidia_icd.json)

set -euo pipefail

PROC_FILE="${CHECK_GPU_PROC_FILE:-/proc/driver/nvidia/version}"
ICD_FILE="${CHECK_GPU_ICD_FILE:-/usr/share/vulkan/icd.d/nvidia_icd.json}"

status=0

# report <text>
# Write one result line to standard output.
report() {
    printf 'check-gpu: %s\n' "$1"
}

# problem <problem> <fix>
# Write the FAIL line and the fix line to standard error. Set the status flag.
problem() {
    printf 'check-gpu: FAIL %s\n' "$1" >&2
    printf 'check-gpu: fix: %s\n' "$2" >&2
    status=1
}

# Read the list of the dynamic linker cache one time, before any search. A
# direct pipe into grep -q can fail under pipefail (SIGPIPE).
if command -v ldconfig >/dev/null 2>&1; then
    ldconfig_cmd=ldconfig
else
    ldconfig_cmd=/sbin/ldconfig
fi
ldconfig_list=$("$ldconfig_cmd" -p 2>/dev/null || true)

# ldconfig_path <soname>
# Print the path of the x86-64 entry with exactly this name. Print nothing when
# no entry exists. The exact match skips i386 entries and longer names.
ldconfig_path() {
    local entry="^[[:space:]]*${1//./\\.}[[:space:]]+\\([^)]*x86-64[^)]*\\)[[:space:]]+=>[[:space:]]+(.+)\$"
    local line
    while IFS= read -r line; do
        if [[ $line =~ $entry ]]; then
            printf '%s\n' "${BASH_REMATCH[1]}"
            return 0
        fi
    done <<<"$ldconfig_list"
    return 0
}

# Item 1: the kernel module version.
kernel_version=
if [ -r "$PROC_FILE" ]; then
    kernel_version=$(head -n 1 -- "$PROC_FILE" | grep -oE '[0-9]+\.[0-9]+(\.[0-9]+)?' | head -n 1 || true)
fi
if [ -n "$kernel_version" ]; then
    report "kernel module $kernel_version"
else
    problem "the NVIDIA kernel module version is not available in $PROC_FILE" \
        "Reboot this machine. If the problem stays, reinstall the NVIDIA driver package."
fi

# Item 2: the user space library version.
# nvidia-smi fails on a mismatch ("Driver/library version mismatch"), which is
# the case that this script must report. So the script reads the file name of
# libnvidia-ml.so.1 in place of the output of nvidia-smi.
user_version=
ml_path=$(ldconfig_path libnvidia-ml.so.1)
if [ -n "$ml_path" ]; then
    ml_file=$(readlink -f -- "$ml_path" || true)
    ml_base=${ml_file##*/}
    if [[ $ml_base == libnvidia-ml.so.* ]]; then
        user_version=${ml_base#libnvidia-ml.so.}
    fi
fi
if [ -n "$user_version" ]; then
    report "user space library $user_version"
else
    problem "the NVIDIA user space library libnvidia-ml.so.1 does not exist" \
        "Install the NVIDIA driver package, for example 'sudo ubuntu-drivers install'."
fi

# Item 3: the Vulkan ICD.
if [ ! -e "$ICD_FILE" ]; then
    problem "the Vulkan ICD file $ICD_FILE does not exist" \
        "Install the NVIDIA driver package with its Vulkan ICD (libnvidia-gl-<branch>)."
else
    icd_library=$(sed -n 's/.*"library_path"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' -- "$ICD_FILE" | head -n 1 || true)
    icd_api=$(sed -n 's/.*"api_version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' -- "$ICD_FILE" | head -n 1 || true)
    resolved=0
    if [ -n "$icd_library" ]; then
        if [[ $icd_library == /* ]]; then
            [ -e "$icd_library" ] && resolved=1
        else
            [ -n "$(ldconfig_path "$icd_library")" ] && resolved=1
        fi
    fi
    report "Vulkan ICD $ICD_FILE (library ${icd_library:-unknown}, API ${icd_api:-unknown})"
    if [ "$resolved" -ne 1 ]; then
        problem "the library of the Vulkan ICD (${icd_library:-unknown}) does not resolve" \
            "Install the NVIDIA driver package with its Vulkan ICD (libnvidia-gl-<branch>)."
    fi
fi

# The version match.
if [ -n "$kernel_version" ] && [ -n "$user_version" ] && [ "$kernel_version" != "$user_version" ]; then
    problem "kernel module $kernel_version does not match user space library $user_version" \
        "Reboot this machine. The reboot loads the kernel module of the installed driver."
fi

if [ "$status" -eq 0 ]; then
    report "OK"
fi
exit "$status"
